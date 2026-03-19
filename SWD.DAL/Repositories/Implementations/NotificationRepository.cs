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
            // Lấy 20 thông báo mới nhất, kèm đầy đủ thông tin địa điểm
            return await _context.Notifications
                .Include(n => n.Rule)
                    .ThenInclude(r => r.Hub)
                        .ThenInclude(h => h.Site)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(long notiId, int? userId = null)
        {
            var query = _context.Notifications.AsQueryable();
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            var noti = await query.FirstOrDefaultAsync(n => n.NotiId == notiId);
            if (noti != null)
            {
                noti.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetUsersBySiteIdAsync(int siteId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.SiteId == siteId || u.Role.RoleName == "ADMIN")
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersByOrgIdAsync(int orgId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.OrgId == orgId || u.Role.RoleName == "ADMIN")
                .ToListAsync();
        }

        public async Task<(List<Notification> Items, int TotalCount)> GetNotificationsHistoryAsync(
            int? userId = null,
            int? siteId = null,
            int? hubId = null,
            string? severity = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            string? sortBy = null,
            string? sortOrder = "desc")
        {
            var query = _context.Notifications
                .Include(n => n.Rule)
                    .ThenInclude(r => r.Hub)
                        .ThenInclude(h => h.Site)
                .AsQueryable();

            // Filters
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            if (siteId.HasValue)
                query = query.Where(n => n.Rule.Hub.SiteId == siteId.Value);

            if (hubId.HasValue)
                query = query.Where(n => n.Rule.HubId == hubId.Value);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(n => n.Rule.Priority == severity);

            if (fromDate.HasValue)
                query = query.Where(n => n.SentAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(n => n.SentAt <= toDate.Value);

            // Total Count
            int totalCount = await query.CountAsync();

            bool isDesc = sortOrder?.ToLower() == "desc";
            query = sortBy?.ToLower() switch {
                "sentat"   => isDesc ? query.OrderByDescending(n => n.SentAt)        : query.OrderBy(n => n.SentAt),
                "severity" => isDesc ? query.OrderByDescending(n => n.Rule != null ? n.Rule.Priority : "") : query.OrderBy(n => n.Rule != null ? n.Rule.Priority : ""),
                "isread"   => isDesc ? query.OrderByDescending(n => n.IsRead)        : query.OrderBy(n => n.IsRead),
                _          => isDesc ? query.OrderByDescending(n => n.SentAt)        : query.OrderBy(n => n.SentAt)
            };

            // Paging
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // ================= COMMON =================
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}