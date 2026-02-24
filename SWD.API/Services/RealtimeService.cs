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
            // Gửi tín hiệu thông báo mới tới toàn bộ clients hoặc cụ thể user
            // Ở đây chúng ta gửi kèm thông tin userId để FE có thể lọc nếu cần, 
            // hoặc dùng Clients.User(userId) nếu đã config Identity.
            await _hubContext.Clients.All.SendAsync("ReceiveAlertNotification", new {
                userId = userId,
                alert = alertData,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
