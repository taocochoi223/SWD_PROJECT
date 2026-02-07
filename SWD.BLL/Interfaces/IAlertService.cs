using SWD.DAL.Models;

namespace SWD.BLL.Interfaces
{
    public interface IAlertService
    {
        Task CheckAndTriggerAlertAsync(SensorData sensorData);
        Task<List<AlertRule>> GetAllRulesAsync();
        Task CreateRuleAsync(AlertRule rule);
        Task<AlertRule?> GetRuleByIdAsync(int ruleId);
        Task UpdateRuleAsync(AlertRule rule);
        Task DeleteRuleAsync(int ruleId);
    }
}
