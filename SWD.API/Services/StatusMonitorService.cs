using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SWD.API.Hubs;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SWD.API.Services
{
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
            _logger.LogInformation("StatusMonitorService started.");

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

            var allHubs = await hubService.GetAllHubsAsync();
            var onlineHubs = allHubs.Where(h => h.IsOnline == true).ToList();

            if (!onlineHubs.Any()) return;

            DateTime vietnamNow;
            try
            {
                vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch
            {
                vietnamNow = DateTime.UtcNow.AddHours(7);
            }

            // Detect offline hubs
            var thresholdTime = vietnamNow.AddSeconds(-OfflineThresholdSeconds);
            var offlineHubs = onlineHubs.Where(h => (h.LastHandshake ?? DateTime.MinValue) < thresholdTime).ToList();

            foreach (var hub in offlineHubs)
            {
                // 1. Update Hub Status in DB
                hub.IsOnline = false;
                await hubService.UpdateHubAsync(hub);

                // 2. Broadcast Hub Status Change (Requirement 2)
                await BroadcastHubStatusChange(hub.HubId, false);

                // 3. Update Sensors Status to Offline
                var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                foreach (var sensor in sensors)
                {
                    if (sensor.Status != "Offline")
                    {
                        await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Offline");
                        
                        // 4. Broadcast Sensor Status Change (Requirement 1)
                        await BroadcastSensorStatusChange(sensor.SensorId, "Offline", hub.HubId);
                    }
                }
            }
        }

        private async Task BroadcastHubStatusChange(int hubId, bool isOnline)
        {
            // For /api/hubs clients
            await _hubContext.Clients.All.SendAsync("ReceiveHubStatusChange", new
            {
                hubId = hubId,
                isOnline = isOnline,
                updatedAt = DateTime.UtcNow
            });
        }

        private async Task BroadcastSensorStatusChange(int sensorId, string status, int hubId)
        {
            // For /api/sensors clients
            await _hubContext.Clients.All.SendAsync("ReceiveSensorStatusChange", new
            {
                sensorId = sensorId,
                status = status,
                hubId = hubId,
                updatedAt = DateTime.UtcNow
            });
        }
    }
}
