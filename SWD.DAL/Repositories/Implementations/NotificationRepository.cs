using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

//ALOALO chú ý mấy thằng nhóc: code này dùng cho các thông báo (Notification) trong hệ thống quản lý sự cố (SWD).
//Nó triển khai các phương thức để tạo, lấy, đánh dấu đã đọc thông báo và tìm người dùng cần nhận thông báo dựa trên site cụ thể.

namespace SWD.DAL.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IoTFinalDbContext _context;

        public NotificationRepository(IoTFinalDbContext context)
        {
            _context = context;
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId)
        {
            // Lấy 20 thông báo mới nhất, kèm đầy đủ thông tin địa điểm và loại cảm biến
            return await _context.Notifications
                .Include(n => n.Rule)
                    .ThenInclude(r => r.Sensor)
                        .ThenInclude(s => s.Hub)
                            .ThenInclude(h => h.Site)
                .Include(n => n.Rule)
                    .ThenInclude(r => r.Sensor)
                        .ThenInclude(s => s.Type)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(long notiId)
        {
            var noti = await _context.Notifications.FindAsync(notiId);
            if (noti != null)
            {
                noti.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetUsersBySiteIdAsync(int siteId)
        {
            // Lấy tất cả Staff của Site đó + Admin tổng (SiteID = null)
            return await _context.Users
                .Where(u => u.SiteId == siteId || u.SiteId == null)
                .ToListAsync();
        }

        // ================= COMMON =================
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}