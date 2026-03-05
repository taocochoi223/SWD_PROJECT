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
                              var newNoti = await _notiService.CreateNotificationAsync(u.UserId, rule.RuleId, message);
                              
                              // Push real-time signal to FE - KHỚP ĐỊNH DẠNG DASHBOARD API
                              await _realtimeService.SendAlertSignalAsync(u.UserId, new {
                                  id = newNoti.NotiId,
                                  sensorName = sensor.Name,
                                  location = $"{sensor.Hub?.Site?.Name} - {sensor.Hub?.Name}",
                                  value = roundedValue,
                                  metricUnit = sensor.Type?.Unit ?? "",
                                  severity = rule.Priority ?? "Info",
                                  status = "Active",
                                  time = newNoti.SentAt
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
