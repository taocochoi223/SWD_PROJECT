using Microsoft.AspNetCore.SignalR;
using SWD.API.Hubs;
using SWD.BLL.Interfaces;

namespace SWD.API.Services
{
    public class RealtimeService : IRealtimeService
    {
        private readonly IHubContext<SensorHub> _hubContext;

        public RealtimeService(IHubContext<SensorHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendAlertSignalAsync(int userId, object alertData)
        {
            // Chỉ gửi thông báo tới user cụ thể (dựa trên UserId trong JWT - ClaimTypes.NameIdentifier hoặc sub)
            // Điều này đảm bảo tính bảo mật, Staff Site A không bao giờ nhận được signal của Site B
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveAlertNotification", new {
                userId = userId,
                alert = alertData,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
