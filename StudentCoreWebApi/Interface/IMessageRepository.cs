using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;

namespace StudentCoreWebApi.Interface
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetMessagesAsync(Guid currentUserId, Guid otherUserId);
        Task<ApiResponse<object>> SendMessageAsync(Guid currentUserId, SendMessageDto dto);
        Task<Dictionary<Guid, Message>> GetLastMessagesForUsersAsync(Guid currentUserId, List<Guid> userIds);
        Task<ApiResponse<UserCurrentProfileDto>> GetUserProfileAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid messageId);
        Task<Message?> GetMessageByIdAsync(Guid messageId);
        Task<bool> ReactToMessageAsync(Guid messageId, string emoji);
        Task<bool> SaveChangesAsync();
    }
}