using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    public interface IAlertRepository
    {
        Task<List<AlertRule>> GetAllRulesAsync();
        Task<List<AlertRule>> GetActiveRulesBySensorIdAsync(int sensorId);
        Task CreateRuleAsync(AlertRule rule);
        Task AddAlertHistoryAsync(AlertHistory history);
        Task<AlertHistory?> GetAlertHistoryByIdAsync(long historyId);
        Task<List<AlertHistory>> GetAlertHistoryAsync( int? sensorId, DateTime? from, DateTime? to );
        Task<List<AlertHistory>> GetAlertHistoryWithFiltersAsync(string? status, string? search);
        Task UpdateAlertHistoryAsync(AlertHistory history);
        Task DeleteAlertHistoryAsync(long historyId);
        Task<AlertRule?> GetRuleByIdAsync(int ruleId);
        Task UpdateRuleAsync(AlertRule rule);
        Task DeleteRuleAsync(int ruleId);
        Task SaveChangesAsync();
    }
}
