using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWD.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;

        public NotificationService(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task CreateNotificationAsync(int userId, int ruleId, string message)
        {
            var noti = new Notification
            {
                UserId = userId,
                RuleId = ruleId,
                Message = message,
                IsRead = false,
                SentAt = DateTime.Now
            };

            await _repo.AddNotificationAsync(noti);
            await _repo.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _repo.GetNotificationsByUserIdAsync(userId);
        }

        public async Task MarkAsReadAsync(long notificationId)
        {
            await _repo.MarkAsReadAsync(notificationId);
        }
    }

}
