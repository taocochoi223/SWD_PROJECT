using SWD.DAL.Models;

namespace SWD.BLL.Interfaces
{
    public interface IAlertService
    {
        Task CheckAndTriggerAlertAsync(Reading reading);
        Task<AlertHistory?> GetAlertByIdAsync(long historyId);
        Task<List<AlertHistory>> GetAlertHistoryAsync(int? sensorId);
        Task<List<AlertHistory>> GetAlertsWithFiltersAsync(string? status, string? search);
        Task ResolveAlertAsync(long historyId);
        Task DeleteAlertAsync(long historyId);
        Task<List<AlertRule>> GetAllRulesAsync();
        Task CreateRuleAsync(AlertRule rule);
        Task<AlertRule?> GetRuleByIdAsync(int ruleId);
        Task UpdateRuleAsync(AlertRule rule);
        Task DeleteRuleAsync(int ruleId);
    }
}
