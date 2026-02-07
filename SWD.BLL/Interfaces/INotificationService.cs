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
        Task CreateNotificationAsync(int userId, int ruleId, string message);
        Task<List<Notification>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(long notificationId);
    }

}
