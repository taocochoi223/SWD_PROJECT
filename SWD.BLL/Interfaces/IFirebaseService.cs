namespace SWD.BLL.Interfaces
{
    public interface IFirebaseService
    {
        Task UpdateSensorDataAsync(string chipId, object data);
        Task UpdateHubStatusAsync(int hubId, bool isOnline);
    }
}
