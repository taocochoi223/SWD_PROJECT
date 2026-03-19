using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SWD.BLL.Services
{
    public class AlertService : IAlertService
    {
        private readonly ISensorRepository _sensorRepo;
        private readonly IAlertRepository _alertRepo;
        private readonly INotificationService _notiService;
        private readonly INotificationRepository _notiRepo;
        private readonly IRealtimeService _realtimeService;
        private readonly IFirebaseService _firebaseService;
        private readonly ILogger<AlertService> _logger;

        public AlertService(
            ISensorRepository sensorRepo,
            IAlertRepository alertRepo,
            INotificationService notiService,
            INotificationRepository notiRepo,
            IRealtimeService realtimeService,
            IFirebaseService firebaseService,
            ILogger<AlertService> logger)
        {
            _sensorRepo = sensorRepo;
            _alertRepo = alertRepo;
            _notiService = notiService;
            _notiRepo = notiRepo;
            _realtimeService = realtimeService;
            _firebaseService = firebaseService;
            _logger = logger;
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
                string ruleName = rule.Name ?? "";
                
                // Identify which field to check based on Rule Name (case-insensitive)
                if (ruleName.Contains("Temperature", StringComparison.OrdinalIgnoreCase) || 
                    ruleName.Contains("Nhiệt độ", StringComparison.OrdinalIgnoreCase)) {
                    if (root.TryGetProperty("v1", out var v1)) numericValue = GetSafeDouble(v1);
                    unit = "°C";
                }
                else if (ruleName.Contains("Humidity", StringComparison.OrdinalIgnoreCase) || 
                         ruleName.Contains("Độ ẩm", StringComparison.OrdinalIgnoreCase)) {
                    if (root.TryGetProperty("v2", out var v2)) numericValue = GetSafeDouble(v2);
                    unit = "%";
                }
                else if (ruleName.Contains("Pressure", StringComparison.OrdinalIgnoreCase) || 
                         ruleName.Contains("Áp suất", StringComparison.OrdinalIgnoreCase)) {
                    if (root.TryGetProperty("v3", out var v3)) numericValue = GetSafeDouble(v3);
                    unit = "hPa";
                }

                if (!numericValue.HasValue) 
                {
                    _logger.LogDebug($"Rule '{ruleName}' skipped: No matching metric (v1/v2/v3) found in JSON.");
                    continue;
                }

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
                    _logger.LogInformation($"[ALERT TRIGGERED] Rule: {rule.Name}, Val: {numericValue}, Limits: {rule.MinVal}-{rule.MaxVal}");
                    
                    // GỬI LÊN FIREBASE NGAY LẬP TỨC ĐỂ REALTIME NHANH NHẤT (Thay vì chờ vòng lặp SQL bên dưới)
                    _ = _firebaseService.UpdateHubAlertAsync(rule.HubId, new
                    {
                        message = message,
                        priority = rule.Priority,
                        time = sensorData.RecordedAt,
                        ruleName = rule.Name
                    });

                    var users = await _notiRepo.GetUsersByOrgIdAsync(rule.OrgId); 
                    if (!users.Any()) _logger.LogWarning($"Alert triggered for rule {rule.Name} but NO USERS found for OrgId {rule.OrgId}");

                    foreach (var u in users)
                    {
                        var newNoti = await _notiService.CreateNotificationAsync(u.UserId, rule.RuleId, rule.OrgId, message);
                        if (newNoti != null)
                        {
                            _logger.LogInformation($"Sending SignalR alert to User {u.UserId} for Rule {rule.Name}");
                            await _realtimeService.SendAlertSignalAsync(u.UserId, newNoti);
                        }
                    }
                }
            }
        }

        private double? GetSafeDouble(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number) return element.GetDouble();
            if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out double val)) return val;
            return null;
        }

        public async Task<List<AlertRule>> GetAllRulesAsync()
        {
            return await _alertRepo.GetAllRulesAsync();
        }

        public async Task<(List<AlertRule> Rules, int TotalCount)> GetAllRulesAsync(string? search, bool? isActive, string? priority, int? siteId, int? hubId, int? pageNumber, int? pageSize, string? sortBy = null, string? sortOrder = "asc")
        {
            var rules = await _alertRepo.GetAllRulesAsync(search, isActive, priority, siteId, hubId, sortBy, sortOrder);
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
