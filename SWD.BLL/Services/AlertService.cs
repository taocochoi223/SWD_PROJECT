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
            // 1. Get Active Rules for this Sensor
            var rules = await _alertRepo.GetActiveRulesBySensorIdAsync(sensorData.SensorId);
            if (rules == null || !rules.Any()) return;

            var sensor = await _sensorRepo.GetSensorByIdAsync(sensorData.SensorId);
            double roundedValue = Math.Round(sensorData.Value, 2);

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
                        message = $"Cảnh báo: Sensor {sensor?.Name ?? sensorData.SensorId.ToString()} vượt ngưỡng (Value: {roundedValue} > Max: {rule.MaxVal})";
                    }
                    else if (rule.MinVal.HasValue && sensorData.Value < rule.MinVal.Value)
                    {
                        isTriggered = true;
                        message = $"Cảnh báo: Sensor {sensor?.Name ?? sensorData.SensorId.ToString()} dưới ngưỡng (Value: {roundedValue} < Min: {rule.MinVal})";
                    }
                }

                if (isTriggered)
                {
                    if (sensor != null && sensor.Hub != null)
                    {
                         var users = await _notiRepo.GetUsersBySiteIdAsync(sensor.Hub.SiteId);
                         foreach (var u in users)
                         {
                              await _notiService.CreateNotificationAsync(u.UserId, rule.RuleId, message);
                              
                              await _realtimeService.SendAlertSignalAsync(u.UserId, new {
                                  ruleName = rule.Name,
                                  sensorName = sensor.Name,
                                  value = roundedValue,
                                  message = message,
                                  priority = rule.Priority,
                                  siteName = sensor.Hub?.Site?.Name
                              });
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
