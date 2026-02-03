using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.BLL.Services
{
    public class AlertService : IAlertService
    {
        private readonly ISensorRepository _sensorRepo;
        private readonly IAlertRepository _alertRepo;
        private readonly INotificationService _notiService;
        private readonly INotificationRepository _notiRepo; // To find users to notify

        public AlertService(
            ISensorRepository sensorRepo,
            IAlertRepository alertRepo,
            INotificationService notiService,
            INotificationRepository notiRepo)
        {
            _sensorRepo = sensorRepo;
            _alertRepo = alertRepo;
            _notiService = notiService;
            _notiRepo = notiRepo;
        }

        public async Task CheckAndTriggerAlertAsync(Reading reading)
        {
            // 1. Get Active Rules for this Sensor
            var rules = await _alertRepo.GetActiveRulesBySensorIdAsync(reading.SensorId);

            if (rules == null || !rules.Any()) return;

            foreach (var rule in rules)
            {
                bool isTriggered = false;
                string message = "";

                // 2. Check Condition
                if (rule.ConditionType == "MinMax")
                {
                    if (rule.MaxVal.HasValue && reading.Value > rule.MaxVal.Value)
                    {
                        isTriggered = true;
                        message = $"Cảnh báo: Sensor {reading.SensorId} vượt ngưỡng cho phép (Value: {reading.Value} > Max: {rule.MaxVal})";
                    }
                    else if (rule.MinVal.HasValue && reading.Value < rule.MinVal.Value)
                    {
                        isTriggered = true;
                        message = $"Cảnh báo: Sensor {reading.SensorId} dưới ngưỡng cho phép (Value: {reading.Value} < Min: {rule.MinVal})";
                    }
                }

                if (isTriggered)
                {
                    var history = new AlertHistory
                    {
                        RuleId = rule.RuleId,
                        SensorId = reading.SensorId,
                        TriggeredAt = DateTime.Now,
                        ValueAtTrigger = reading.Value,
                        Severity = rule.Priority,
                        Message = message
                    };

                    await _alertRepo.AddAlertHistoryAsync(history);
                    await _alertRepo.SaveChangesAsync(); 

                    var sensor = await _sensorRepo.GetSensorByIdAsync(reading.SensorId);
                    if (sensor != null && sensor.Hub != null)
                    {
                         var users = await _notiRepo.GetUsersBySiteIdAsync(sensor.Hub.SiteId);
                         foreach (var u in users)
                         {
                             await _notiService.CreateNotificationAsync(u.UserId, history.HistoryId, message);
                         }
                    }
                }
            }
        }

        public async Task<List<AlertHistory>> GetAlertHistoryAsync(int? sensorId)
        {
            return await _alertRepo.GetAlertHistoryAsync(sensorId, null, null);
        }

        public async Task<AlertHistory?> GetAlertByIdAsync(long historyId)
        {
            return await _alertRepo.GetAlertHistoryByIdAsync(historyId);
        }

        public async Task<List<AlertHistory>> GetAlertsWithFiltersAsync(string? status, string? search)
        {
            return await _alertRepo.GetAlertHistoryWithFiltersAsync(status, search);
        }

        public async Task ResolveAlertAsync(long historyId)
        {
            var alert = await _alertRepo.GetAlertHistoryByIdAsync(historyId);
            if (alert != null)
            {
                alert.ResolvedAt = DateTime.UtcNow;
                await _alertRepo.UpdateAlertHistoryAsync(alert);
                await _alertRepo.SaveChangesAsync();
            }
        }

        public async Task DeleteAlertAsync(long historyId)
        {
            await _alertRepo.DeleteAlertHistoryAsync(historyId);
            await _alertRepo.SaveChangesAsync();
        }

        public async Task<List<AlertRule>> GetAllRulesAsync()
        {
            return await _alertRepo.GetAllRulesAsync();
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
