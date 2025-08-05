using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace StudentCoreWebApi.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

        public async Task SendPrivateMessage(string receiverUserId, string message)
        {
            var senderUserId = Context.UserIdentifier;
            if (senderUserId != null)
            {
                await Clients.User(receiverUserId).SendAsync("ReceivePrivateMessage", senderUserId, message);
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                OnlineUsers.TryAdd(userId, Context.ConnectionId);
                await Clients.All.SendAsync("UserStatusChanged", userId, "Active");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                OnlineUsers.TryRemove(userId, out _);
                await Clients.All.SendAsync("UserStatusChanged", userId, "Inactive");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<string>> GetOnlineUsers()
        {
            return Task.FromResult(OnlineUsers.Keys.ToList());
        }

        public async Task NotifyMessageRead(Guid senderId, Guid messageId)
        {
            var receiverId = Context.UserIdentifier;

            if (Guid.TryParse(receiverId, out _))
            {
                await Clients.User(senderId.ToString())
                             .SendAsync("MessageRead", messageId);
            }
        }

        public async Task SendOffer(string receiverId, string offer)
        {
            var senderName = Context.User?.Identity?.Name ?? "Unknown";
            await Clients.User(receiverId).SendAsync("ReceiveOffer", Context.UserIdentifier, offer, senderName);
        }

        public async Task SendAnswer(string receiverId, string answer)
        {
            await Clients.User(receiverId).SendAsync("ReceiveAnswer", Context.UserIdentifier, answer);
        }

        public async Task SendIceCandidate(string receiverId, string candidate)
        {
            await Clients.User(receiverId).SendAsync("ReceiveIceCandidate", Context.UserIdentifier, candidate);
        }

        public async Task SendEndCall(string receiverId)
        {
            await Clients.User(receiverId).SendAsync("ReceiveEndCall");
        }

        public async Task SendMessageReaction(string receiverId, Guid messageId, string emoji)
        {
            await Clients.User(receiverId).SendAsync("MessageReacted", messageId, emoji);
        }
    }
}