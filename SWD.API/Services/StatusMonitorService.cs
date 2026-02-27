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

            var (allHubs, _) = await hubService.GetAllHubsAsync();
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

            // Step 1: Set all hubs OFFLINE first (in parallel)
            var hubOfflineTasks = offlineHubs.Select(async hub =>
            {
                using var innerScope = _scopeFactory.CreateScope();
                var innerHubService = innerScope.ServiceProvider.GetRequiredService<IHubService>();

                hub.IsOnline = false;
                await innerHubService.UpdateHubAsync(hub);
                await BroadcastHubStatusChange(hub.HubId, false, hub.LastHandshake);
            });
            await Task.WhenAll(hubOfflineTasks);

            // Step 2: THEN set all sensors OFFLINE (in parallel)
            var sensorOfflineTasks = offlineHubs.Select(async hub =>
            {
                using var innerScope = _scopeFactory.CreateScope();
                var innerSensorService = innerScope.ServiceProvider.GetRequiredService<ISensorService>();

                var sensors = await innerSensorService.GetSensorsByHubIdAsync(hub.HubId);
                var tasks = sensors
                    .Where(s => s.Status != "Offline")
                    .Select(async sensor =>
                    {
                        await innerSensorService.UpdateSensorStatusAsync(sensor.SensorId, "Offline");
                        await BroadcastSensorStatusChange(sensor.SensorId, "Offline", hub.HubId);
                    });
                await Task.WhenAll(tasks);
            });
            await Task.WhenAll(sensorOfflineTasks);
        }

        private async Task BroadcastHubStatusChange(int hubId, bool isOnline, DateTime? lastHandshake = null)
        {
            // For /api/hubs clients
            await _hubContext.Clients.All.SendAsync("ReceiveHubStatusChange", new
            {
                hubId = hubId,
                isOnline = isOnline,
                lastHandshake = lastHandshake,
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
