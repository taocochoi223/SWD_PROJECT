using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    public interface IAlertRepository
    {
        Task<List<AlertRule>> GetActiveRulesBySensorIdAsync(int sensorId);
        Task<List<AlertRule>> GetAllRulesAsync();
        Task CreateRuleAsync(AlertRule rule);
        Task<AlertRule?> GetRuleByIdAsync(int ruleId);
        Task UpdateRuleAsync(AlertRule rule);
        Task DeleteRuleAsync(int ruleId);
        Task SaveChangesAsync();
    }
}
