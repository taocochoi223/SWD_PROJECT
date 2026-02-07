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

        public async Task CheckAndTriggerAlertAsync(SensorData sensorData)
        {
            // 1. Get Active Rules for this Sensor
            var rules = await _alertRepo.GetActiveRulesBySensorIdAsync(sensorData.SensorId);

            if (rules == null || !rules.Any()) return;

            foreach (var rule in rules)
            {
                bool isTriggered = false;
                string message = "";

                // 2. Check Condition
                if (rule.ConditionType == "MinMax")
                {
                    if (rule.MaxVal.HasValue && sensorData.Value > rule.MaxVal.Value)
                    {
                        isTriggered = true;
                        message = $"Cảnh báo: Sensor {sensorData.SensorId} vượt ngưỡng cho phép (Value: {sensorData.Value} > Max: {rule.MaxVal})";
                    }
                    else if (rule.MinVal.HasValue && sensorData.Value < rule.MinVal.Value)
                    {
                        isTriggered = true;
                        message = $"Cảnh báo: Sensor {sensorData.SensorId} dưới ngưỡng cho phép (Value: {sensorData.Value} < Min: {rule.MinVal})";
                    }
                }

                if (isTriggered)
                {
                    
                    var sensor = await _sensorRepo.GetSensorByIdAsync(sensorData.SensorId);
                    if (sensor != null && sensor.Hub != null)
                    {
                         var users = await _notiRepo.GetUsersBySiteIdAsync(sensor.Hub.SiteId);
                         foreach (var u in users)
                         {
                             // Create notification directly linked to the Rule
                             // Note: NotificationService might need updating to accept RuleId instead of HistoryId, 
                             // but for now we assume CreateNotificationAsync handles logic or we adjust parameters.
                             // Assuming CreateNotificationAsync takes (userId, sourceId, message) - we might need to check its signature.
                             // Based on previous file reads, Notification has RuleId.
                             await _notiService.CreateNotificationAsync(u.UserId, rule.RuleId, message);
                         }
                    }
                }
            }
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
