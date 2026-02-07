using Microsoft.AspNetCore.SignalR;
using SWD.API.Hubs;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;

namespace SWD.API.Services
{
    /// <summary>
    /// Background service to monitor Hub status and auto-detect offline devices
    /// </summary>
    public class StatusMonitorService : BackgroundService
    {
        private readonly ILogger<StatusMonitorService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly IConfiguration _configuration;

        private int CheckIntervalSeconds => int.Parse(_configuration["StatusMonitor:CheckIntervalSeconds"] ?? "10");
        private int OfflineThresholdSeconds => int.Parse(_configuration["StatusMonitor:OfflineThresholdSeconds"] ?? "15");

        public StatusMonitorService(
            ILogger<StatusMonitorService> logger,
            IServiceScopeFactory scopeFactory,
            IHubContext<SensorHub> hubContext,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"StatusMonitorService started. Check interval: {CheckIntervalSeconds}s, Offline threshold: {OfflineThresholdSeconds}s");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndUpdateHubStatus();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in StatusMonitorService");
                }

                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
            }
        }

        private async Task CheckAndUpdateHubStatus()
        {
            using var scope = _scopeFactory.CreateScope();
            var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
            var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();

            // Get all hubs that are currently marked as online
            var allHubs = await hubService.GetAllHubsAsync();
            var onlineHubs = allHubs.Where(h => h.IsOnline == true).ToList();

            if (!onlineHubs.Any())
            {
                _logger.LogDebug("No online hubs to check");
                return;
            }

            DateTime vietnamNow;
            try 
            {
                vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch
            {
                try 
                {
                     vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"));
                }
                catch
                {
                     vietnamNow = DateTime.UtcNow.AddHours(7);
                }
            }
            
            var offlineThresholdIdx = vietnamNow.AddSeconds(-OfflineThresholdSeconds);
             // Compare with local time since DB stores local time
            var hubsToMarkOffline = onlineHubs.Where(h => (h.LastHandshake ?? DateTime.MinValue) < offlineThresholdIdx).ToList();

            if (!hubsToMarkOffline.Any())
            {
                _logger.LogDebug($"All {onlineHubs.Count} online hubs are active");
                return;
            }

            foreach (var hub in hubsToMarkOffline)
            {
                try
                {
                    // Calculate how long hub has been inactive
                    var inactiveMinutes = (vietnamNow - (hub.LastHandshake ?? vietnamNow)).TotalMinutes;
                    
                    _logger.LogWarning($"Hub {hub.HubId} ({hub.Name}) inactive for {inactiveMinutes:F1} minutes. Marking as offline.");

                    // Update hub status
                    hub.IsOnline = false;
                    await hubService.UpdateHubAsync(hub);

                    // Auto-set all sensors of this hub to Offline
                    var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                    var onlineSensorsCount = 0;
                    
                    foreach (var sensor in sensors)
                    {
                        if (sensor.Status == "Online")
                        {
                            onlineSensorsCount++;
                            await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Offline");
                            sensor.Status = "Offline";
                            await BroadcastSensorOffline(sensor, hub.HubId);
                        }
                    }

                    // Broadcast hub offline status via SignalR
                    await BroadcastHubStatus(hub, sensors.Count);

                    _logger.LogInformation($"Hub {hub.HubId} marked offline. {sensors.Count} sensors affected ({onlineSensorsCount} set to Offline).");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error marking hub {hub.HubId} as offline");
                }
            }
        }

        private async Task BroadcastHubStatus(DAL.Models.Hub hub, int sensorCount)
        {
            var statusData = new
            {
                hubId = hub.HubId,
                hubName = hub.Name,
                macAddress = hub.MacAddress,
                isOnline = hub.IsOnline,
                lastHandshake = hub.LastHandshake,
                sensorCount = sensorCount,
                timestamp = DateTime.UtcNow
            };

            // Broadcast to all clients
            await _hubContext.Clients.All.SendAsync("ReceiveHubStatus", statusData);

            // Also broadcast to hub-specific group
            await _hubContext.Clients.Group($"hub_{hub.HubId}").SendAsync("ReceiveHubOffline", statusData);
        }

        private async Task BroadcastSensorOffline(Sensor sensor, int hubId)
        {
            var sensorData = new
            {
                hubId = hubId,
                sensorId = sensor.SensorId,
                sensorName = sensor.Name,
                typeName = sensor.Type?.TypeName ?? "Unknown",
                currentValue = 0,
                unit = sensor.Type?.Unit ?? "",
                status = "Offline",
                lastUpdate = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("ReceiveSensorUpdate", sensorData);
            await _hubContext.Clients.Group($"hub_{hubId}").SendAsync("ReceiveSensorUpdate", sensorData);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StatusMonitorService stopping");
            return base.StopAsync(cancellationToken);
        }
    }
}
