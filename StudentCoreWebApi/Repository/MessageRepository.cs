using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.Data;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Hubs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class MessageRepository : IMessageRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IConfiguration _config;


    public MessageRepository(ApplicationDbContext context, IHubContext<ChatHub> hubContext, IConfiguration config)
    {
        _context = context;
        _hubContext = hubContext;
        _config = config;
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(Guid currentUserId, Guid otherUserId)
    {
        return await _context.Messages
            .Where(m =>
                (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.SendAt)
            .ToListAsync();
    }

    public async Task<ApiResponse<object>> SendMessageAsync(Guid currentUserId, SendMessageDto dto)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = dto.ReceiverId,
            Content = dto.Content,
            SendAt = DateTime.UtcNow.ToLocalTime()
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        //await _hubContext.Clients.User(dto.ReceiverId.ToString())
        //    .SendAsync("ReceivePrivateMessage", currentUserId.ToString(), dto.Content);

        await _hubContext.Clients.User(dto.ReceiverId.ToString())
                .SendAsync("ReceivePrivateMessage", new
                {
                    message.Id,
                    message.SenderId,
                    message.ReceiverId,
                    message.Content,
                    message.SendAt,
                    message.IsRead
                });


        return new ApiResponse<object>(true, "Message sent successfully", new
        {
            message.Id,
            message.SenderId,
            message.ReceiverId,
            message.Content,
            message.SendAt
        });
    }

    public async Task<Dictionary<Guid, Message>> GetLastMessagesForUsersAsync(Guid currentUserId, List<Guid> userIds)
    {
        return await _context.Messages
            .Where(m =>
                (userIds.Contains(m.ReceiverId) && m.SenderId == currentUserId) ||
                (userIds.Contains(m.SenderId) && m.ReceiverId == currentUserId))
            .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
            .Select(g => g.OrderByDescending(m => m.SendAt).First())
            .ToDictionaryAsync(
                m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId,
                m => m
            );
    }

    public async Task<ApiResponse<UserCurrentProfileDto>> GetUserProfileAsync(Guid userId)
    {
        var baseUrl = _config["AppBaseUrl"];

        var user = await _context.Users
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new UserCurrentProfileDto
            {
                FirstName = u.FirstName,
                LastName = u.LastName,
                Phone = u.Phone,
                Email = u.Email,
                Description = u.Description,
                Status = u.Status.ToString(),
                ProfileImage = !string.IsNullOrEmpty(u.ProfileImage)
                ? $"{baseUrl}/profile-images/{u.ProfileImage}"
                : null,
                IsPremium = u.IsPremium,
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return new ApiResponse<UserCurrentProfileDto>(false, "User not found", null);

        return new ApiResponse<UserCurrentProfileDto>(true, "Profile fetched successfully", user);
    }

    public async Task<bool> MarkAsReadAsync(Guid messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null) return false;

        message.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Message?> GetMessageByIdAsync(Guid messageId)
    {
        return await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ReactToMessageAsync(Guid messageId, string emoji)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null) return false;

        message.Reaction = emoji;
        return await SaveChangesAsync();
    }

}