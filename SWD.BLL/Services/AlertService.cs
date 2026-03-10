using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;
using System.Text.Json;

namespace SWD.BLL.Services
{
    public class AlertService : IAlertService
    {
        private readonly ISensorRepository _sensorRepo;
        private readonly IAlertRepository _alertRepo;
        private readonly INotificationService _notiService;
        private readonly INotificationRepository _notiRepo;
        private readonly IRealtimeService _realtimeService;

        public AlertService(
            ISensorRepository sensorRepo,
            IAlertRepository alertRepo,
            INotificationService notiService,
            INotificationRepository notiRepo,
            IRealtimeService realtimeService)
        {
            _sensorRepo = sensorRepo;
            _alertRepo = alertRepo;
            _notiService = notiService;
            _notiRepo = notiRepo;
            _realtimeService = realtimeService;
        }

        public async Task CheckAndTriggerAlertAsync(SensorData sensorData)
        {
            // 1. Get Active Rules for this Hub
            var rules = await _alertRepo.GetActiveRulesByHubIdAsync(sensorData.HubId);
            if (rules == null || !rules.Any()) return;

            // 2. Parse JSON
            using var doc = JsonDocument.Parse(sensorData.JsonValue);
            var root = doc.RootElement;
            
            foreach (var rule in rules)
            {
                double? numericValue = null;
                string unit = "";
                
                // Identify which field to check based on Rule Name
                if (rule.Name!.Contains("Temperature") || rule.Name.Contains("Nhiệt độ")) {
                    if (root.TryGetProperty("v1", out var v1)) numericValue = v1.GetDouble();
                    unit = "°C";
                }
                else if (rule.Name.Contains("Humidity") || rule.Name.Contains("Độ ẩm")) {
                    if (root.TryGetProperty("v2", out var v2)) numericValue = v2.GetDouble();
                    unit = "%";
                }
                else if (rule.Name.Contains("Pressure") || rule.Name.Contains("Áp suất")) {
                    if (root.TryGetProperty("v3", out var v3)) numericValue = v3.GetDouble();
                    unit = "hPa";
                }

                if (!numericValue.HasValue) continue;

                bool isTriggered = false;
                string message = "";

                // MinMax logic (User showns MinMax condition in SQL but rule names are usually specific)
                // If ConditionType is MinMax or Range
                if ((rule.ConditionType == "MinMax" || rule.ConditionType == "Range") && rule.MinVal.HasValue && rule.MaxVal.HasValue)
                {
                    if (numericValue < rule.MinVal.Value || numericValue > rule.MaxVal.Value)
                    {
                        isTriggered = true;
                        message = $"Cảnh báo '{rule.Name}': {numericValue}{unit} nằm ngoài ngưỡng ({rule.MinVal.Value} - {rule.MaxVal.Value})";
                    }
                }
                else if (rule.ConditionType == "Greater" && rule.MaxVal.HasValue && numericValue > rule.MaxVal.Value)
                {
                    isTriggered = true;
                    message = $"Cảnh báo '{rule.Name}': {numericValue}{unit} vượt ngưỡng tối đa {rule.MaxVal.Value}";
                }
                else if (rule.ConditionType == "Less" && rule.MinVal.HasValue && numericValue < rule.MinVal.Value)
                {
                    isTriggered = true;
                    message = $"Cảnh báo '{rule.Name}': {numericValue}{unit} thấp hơn ngưỡng tối thiểu {rule.MinVal.Value}";
                }

                if (isTriggered)
                {
                    // OrgId is now available on Rule
                    var users = await _notiRepo.GetUsersByOrgIdAsync(rule.OrgId); 
                    foreach (var u in users)
                    {
                        var newNoti = await _notiService.CreateNotificationAsync(u.UserId, rule.RuleId, message);
                        if (newNoti != null)
                        {
                            await _realtimeService.SendAlertSignalAsync(u.UserId, newNoti);
                        }
                    }
                }
            }
        }

        public async Task<List<AlertRule>> GetAllRulesAsync()
        {
            return await _alertRepo.GetAllRulesAsync();
        }

        public async Task<(List<AlertRule> Rules, int TotalCount)> GetAllRulesAsync(string? search, bool? isActive, string? priority, int? siteId, int? pageNumber, int? pageSize, string? sortBy = null, string? sortOrder = "asc")
        {
            var rules = await _alertRepo.GetAllRulesAsync(search, isActive, priority, siteId, sortBy, sortOrder);
            var totalCount = rules.Count;

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                rules = rules.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
            }

            return (rules, totalCount);
        }

        public async Task CreateRuleAsync(AlertRule rule)
        {
            await _alertRepo.CreateRuleAsync(rule);
            await _alertRepo.SaveChangesAsync();
        }

        public async Task<AlertRule?> GetRuleByIdAsync(int ruleId)
        {
            return await _alertRepo.GetRuleByIdAsync(ruleId);
        }

        public async Task UpdateRuleAsync(AlertRule rule)
        {
            await _alertRepo.UpdateRuleAsync(rule);
            await _alertRepo.SaveChangesAsync();
        }

        public async Task DeleteRuleAsync(int ruleId)
        {
            await _alertRepo.DeleteRuleAsync(ruleId);
            await _alertRepo.SaveChangesAsync();
        }
    }
}
