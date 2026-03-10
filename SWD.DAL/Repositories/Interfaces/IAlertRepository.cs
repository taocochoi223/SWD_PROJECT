using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    public interface IAlertRepository
    {
        Task<List<AlertRule>> GetActiveRulesByHubIdAsync(int hubId);
        Task<List<AlertRule>> GetAllRulesAsync();
        Task<List<AlertRule>> GetAllRulesAsync(string? search, bool? isActive, string? priority, int? siteId = null, string? sortBy = null, string? sortOrder = "asc");
        Task CreateRuleAsync(AlertRule rule);
        Task<AlertRule?> GetRuleByIdAsync(int ruleId);
        Task UpdateRuleAsync(AlertRule rule);
        Task DeleteRuleAsync(int ruleId);
        Task SaveChangesAsync();
    }
}
