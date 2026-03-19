using SWD.DAL.Models;

//ALOALO chú ý mấy thằng nhóc: code này dùng cho các thông báo (Notification) trong hệ thống quản lý sự cố (SWD).
//Nó định nghĩa các phương thức để tạo, lấy, đánh dấu đã đọc thông báo và tìm người dùng cần nhận thông báo dựa trên site cụ thể.

namespace SWD.DAL.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        // 1. Tạo thông báo mới (Khi có sự cố Alert -> Tạo Noti cho User)
        Task AddNotificationAsync(Notification notification);

        // 2. Lấy danh sách thông báo của 1 User (Để hiện lên App Flutter)
        Task<List<Notification>> GetNotificationsByUserIdAsync(int userId);

        // 3. Đánh dấu đã đọc (Khi user bấm vào xem)
        Task MarkAsReadAsync(long notiId, int? userId = null);

        // 4. Tìm danh sách User cần nhận thông báo (VD: Lỗi ở Site A thì chỉ gửi cho Staff Site A)
        Task<List<User>> GetUsersBySiteIdAsync(int siteId);
        Task<List<User>> GetUsersByOrgIdAsync(int orgId);

        // 5. Lấy lịch sử thông báo có phân trang và lọc
        Task<(List<Notification> Items, int TotalCount)> GetNotificationsHistoryAsync(
            int? userId = null,
            int? siteId = null,
            int? hubId = null,
            string? severity = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            string? sortBy = null,
            string? sortOrder = "desc");

        // ================= COMMON =================
        Task SaveChangesAsync();
    }
}