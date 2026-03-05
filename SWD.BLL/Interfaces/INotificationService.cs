using SWD.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWD.BLL.Interfaces
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(int userId, int ruleId, string message);
        Task<List<Notification>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(long notificationId, int? userId = null);
        Task<(List<Notification> Items, int TotalCount)> GetNotificationsHistoryAsync(
            int? userId = null,
            int? siteId = null,
            int? sensorId = null,
            string? severity = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            string? sortBy = null,
            string? sortOrder = "desc");
    }

}
