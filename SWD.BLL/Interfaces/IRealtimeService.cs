namespace SWD.BLL.Interfaces
{
    public interface IRealtimeService
    {
        /// <summary>
        /// Gửi tín hiệu cảnh báo môi trường tới Frontend qua SignalR
        /// </summary>
        Task SendAlertSignalAsync(int userId, object alertData);
    }
}
