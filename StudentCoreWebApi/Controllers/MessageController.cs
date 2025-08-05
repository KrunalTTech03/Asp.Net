using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Razorpay.Api;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Hubs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IMessageRepository _menurepository;
    private readonly IUserRepository _userRepository;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IConfiguration _config;

    public MessageController(IMessageRepository repository, IHubContext<ChatHub> hub, IConfiguration config, IUserRepository userrepository)
    {
        _menurepository = repository;
        _hub = hub;
        _config = config;
        _userRepository = userrepository;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var result = await _menurepository.SendMessageAsync(currentUserId, dto);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("history/{receiverId:guid}")]
    public async Task<IActionResult> GetChatHistory(Guid receiverId)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var messages = await _menurepository.GetMessagesAsync(currentUserId, receiverId);
        return Ok(messages);
    }

    [HttpPost("last-messages")]
    public async Task<IActionResult> GetLastMessagesForUsers([FromBody] List<Guid> userIds)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var result = await _menurepository.GetLastMessagesForUsersAsync(currentUserId, userIds);
        return Ok(result);
    }

    [HttpGet("current-user-profile")]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var result = await _menurepository.GetUserProfileAsync(currentUserId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("user-profile/{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var result = await _menurepository.GetUserProfileAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("mark-as-read")]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkReadDto dto)
    {
        var message = await _menurepository.GetMessageByIdAsync(dto.MessageId);
        if (message == null)
            return NotFound("Message not found.");

        var success = await _menurepository.MarkAsReadAsync(dto.MessageId);
        if (!success)
            return BadRequest("Failed to update.");

        await _hub.Clients.User(message.SenderId.ToString())
                  .SendAsync("MessageRead", dto.MessageId);

        return Ok();
    }

    [HttpPost("react")]
    public async Task<IActionResult> ReactToMessage([FromBody] MessageReactionDto dto)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var user = await _userRepository.GetUserByIdAsync(currentUserId);
        if (user == null || !user.IsPremium)
            return StatusCode(403, new { Message = "Premium access required to react to messages." });

        var success = await _menurepository.ReactToMessageAsync(dto.MessageId, dto.Emoji);
        if (!success)
            return NotFound("Message not found.");

        var message = await _menurepository.GetMessageByIdAsync(dto.MessageId);
        if (message != null)
        {
            var receiverId = message.ReceiverId.ToString();
            await _hub.Clients.User(receiverId)
                .SendAsync("MessageReacted", dto.MessageId, dto.Emoji);
        }

        return Ok(new { success = true, reaction = dto.Emoji });
    }



    [HttpPost("create-payment-order")]
    public IActionResult CreatePaymentOrder([FromBody] PaymentRequestDto request)
    {
        var key = _config["Razorpay:Key"];
        var secret = _config["Razorpay:Secret"];

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
            return StatusCode(500, "Razorpay credentials are missing.");

        RazorpayClient client = new RazorpayClient(key, secret);

        var options = new Dictionary<string, object>
    {
        { "amount", request.Amount * 100 },
        { "currency", "INR" },
        { "receipt", $"rcpt_{Guid.NewGuid().ToString("N").Substring(0, 20)}" },
        { "payment_capture", 1 }
    };

        try
        {
            var order = client.Order.Create(options);
            return Ok(new { orderId = order["id"].ToString() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message, ErrorType = "BadRequestError" });
        }
    }

}