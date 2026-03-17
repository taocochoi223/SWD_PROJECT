namespace SWD.BLL.Interfaces
{
    public interface IFirebaseService
    {
        Task UpdateSensorDataAsync(string chipId, object data);
        Task UpdateHubStatusAsync(int hubId, bool isOnline);
        Task UpdateHubDataAsync(int hubId, object data);
        Task UpdateHubAlertAsync(int hubId, object alert);
    }
}
