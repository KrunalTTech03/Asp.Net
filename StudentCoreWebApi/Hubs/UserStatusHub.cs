using Microsoft.AspNetCore.SignalR;

namespace StudentCoreWebApi.Hubs
{
    public class UserStatusHub : Hub
    { 
        public async Task SendStatusUpdate(Guid userId, string status)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", userId, status);
        }
    }
}
