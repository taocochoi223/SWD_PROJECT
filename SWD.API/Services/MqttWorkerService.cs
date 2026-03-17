using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using HubModel = SWD.DAL.Models.Hub;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SWD.API.Hubs;

namespace SWD.API.Services
{
    public class MqttWorkerService : BackgroundService
    {
        private readonly ILogger<MqttWorkerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly IFirebaseService _firebaseService;

        private IMqttClient _mqttClient = null!;
        private MqttClientOptions _mqttOptions = null!;

        // Lock để tránh race condition: status message và data message không được xử lý đồng thời
        private readonly SemaphoreSlim _statusLock = new SemaphoreSlim(1, 1);

        // Cờ đánh dấu gateway đã tắt → bỏ qua data cũ buffered
        private volatile bool _gatewayOffline = false;

        private string Broker => _configuration["MqttSettings:Broker"] ?? "mqtt1.eoh.io";
        private int Port => int.Parse(_configuration["MqttSettings:Port"] ?? "1883");
        private string GatewayToken => _configuration["MqttSettings:GatewayToken"] ?? "";
        private string DataTopic => $"eoh/chip/{GatewayToken}/third_party/+/data";
        private string StatusTopic => $"eoh/chip/{GatewayToken}/is_online";

        public MqttWorkerService(
            ILogger<MqttWorkerService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IHubContext<SensorHub> hubContext,
            IFirebaseService firebaseService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _hubContext = hubContext;
            _firebaseService = firebaseService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(Broker, Port)
                .WithCredentials(GatewayToken, GatewayToken)
                .WithCleanSession()
                .Build();

            _mqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
            _mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

            await base.StartAsync(cancellationToken);
        }

        private async Task ConnectToMqttAsync(CancellationToken cancellationToken = default)
        {
            while (!_mqttClient.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to MQTT Broker. Retrying in 5 seconds...");
                    await Task.Delay(5000, cancellationToken).ContinueWith(_ => { });
                }
            }
        }

        private async Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            _logger.LogInformation("Connected to MQTT Broker.");

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(DataTopic))
                .WithTopicFilter(f => f.WithTopic(StatusTopic))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);
            _logger.LogInformation($"Subscribed to data topic: {DataTopic}");
            _logger.LogInformation($"Subscribed to status topic: {StatusTopic}");
        }

        private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            _logger.LogWarning("Disconnected from MQTT Broker. Attempting to reconnect...");
            await Task.Delay(5000);
            await ConnectToMqttAsync();
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
            _logger.LogInformation($"Received Message on {topic}: {payload}");

            if (e.ApplicationMessage.Retain)
            {
                _logger.LogWarning($"[MQTT] Skipping retained message on topic: {topic}");
                return;
            }

            if (topic == StatusTopic)
            {
                await HandleGatewayStatusMessage(payload);
                return;
            }

            string[] segments = topic.Split('/');
            if (segments.Length < 6) return;
            string chipId = segments[4];

            await ProcessDataMessage(chipId, payload);
        }

        private async Task HandleGatewayStatusMessage(string payload)
        {
            await _statusLock.WaitAsync();
            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("ol", out var olValue)) return;

                int ol = olValue.GetInt32();
                bool isOnline = ol == 1;

                _logger.LogInformation($"[GatewayStatus] ol={ol} → gateway {(isOnline ? "ONLINE" : "OFFLINE")}");

                using var scope = _scopeFactory.CreateScope();
                var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
                var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
                var systemLogService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

                await systemLogService.LogOptionAsync("MQTT-Gateway", $"Gateway {(isOnline ? "ONLINE" : "OFFLINE")} | Payload: {payload}");

                DateTime vietnamNow;
                try { vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); }
                catch { vietnamNow = DateTime.UtcNow.AddHours(7); }

                var (allHubs, _) = await hubService.GetAllHubsAsync();

                if (isOnline)
                {
                    _gatewayOffline = false; // Gateway bật → cho phép xử lý data
                    var hubsToNotify = new List<HubModel>();
                    foreach (var hub in allHubs)
                    {
                        bool wasOffline = hub.IsOnline != true;
                        hub.LastHandshake = vietnamNow;
                        hub.IsOnline = true;
                        await hubService.UpdateHubAsync(hub);
                        await _firebaseService.UpdateHubStatusAsync(hub.HubId, true);
                        if (wasOffline) hubsToNotify.Add(hub);
                    }

                    foreach (var hub in hubsToNotify)
                    {
                        await BroadcastHubStatusChange(hub.HubId, true, vietnamNow);
                        _logger.LogInformation($"[GatewayStatus] Hub {hub.HubId} ({hub.Name}) → ONLINE");

                        var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                        foreach (var sensor in sensors.Where(s => s.Status != "Online"))
                        {
                            await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Online");
                            await BroadcastSensorStatusChange(sensor.SensorId, "Online", hub.HubId);
                        }
                    }
                }
                else
                {
                    // {"ol":0} = Gateway tắt → offline TẤT CẢ hub ngay lập tức
                    _gatewayOffline = true; // Chặn data cũ buffered
                    var onlineHubs = allHubs.Where(h => h.IsOnline == true).ToList();
                    
                    foreach (var hub in onlineHubs)
                    {
                        hub.IsOnline = false;
                        await hubService.UpdateHubAsync(hub);
                        await _firebaseService.UpdateHubStatusAsync(hub.HubId, false);
                        await BroadcastHubStatusChange(hub.HubId, false, hub.LastHandshake);
                        _logger.LogInformation($"[GatewayStatus] Hub {hub.HubId} ({hub.Name}) → OFFLINE");

                        var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                        foreach (var sensor in sensors.Where(s => s.Status != "Offline"))
                        {
                            await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Offline");
                            await BroadcastSensorStatusChange(sensor.SensorId, "Offline", hub.HubId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GatewayStatus] Error processing gateway status message");
            }
            finally
            {
                _statusLock.Release();
            }
        }

        private async Task ProcessDataMessage(string chipId, string payload)
        {
            await _statusLock.WaitAsync();
            try
            {
                // Nếu gateway đã tắt → bỏ qua data cũ buffered
                if (_gatewayOffline)
                {
                    _logger.LogWarning($"[MQTT] Skipping buffered data (gateway offline) - chipId: {chipId}");
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
                var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
                var systemLogService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

                var data = JsonSerializer.Deserialize<EohWebhookDto>(payload);
                if (data == null) return;

                await systemLogService.LogOptionAsync("MQTT-Listener", $"ChipId: {chipId} | Payload: {payload}");

                string macAddress = data.v12 ?? chipId;
                var hub = await hubService.GetHubByMacAsync(macAddress);
                if (hub == null)
                {
                    _logger.LogWarning($"[MQTT] No hub found for MAC: {macAddress} (chipId: {chipId})");
                    return;
                }

                DateTime vietnamNow;
                try
                {
                    vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                }
                catch
                {
                    vietnamNow = DateTime.UtcNow.AddHours(7);
                }

                bool wasOffline = hub.IsOnline != true;
                hub.IsOnline = true;
                hub.LastHandshake = vietnamNow;

                if ((!string.IsNullOrEmpty(data.v5) || !string.IsNullOrEmpty(data.v6)) && hub.Site != null)
                {
                    string newAddress = $"{data.v6}, {data.v5}".Trim(',', ' ');
                    if (!string.IsNullOrEmpty(newAddress)) hub.Site.Address = newAddress;
                }

                await hubService.UpdateHubAsync(hub);

                if (wasOffline)
                {
                    _logger.LogInformation($"[MQTT] Hub {hub.HubId} ({hub.Name}) → ONLINE (MAC: {macAddress})");
                    await BroadcastHubStatusChange(hub.HubId, true, hub.LastHandshake);
                }

                await BroadcastHubEnvironmentData(hub.HubId, data.v1, data.v2, data.v3);

                // Gửi dữ liệu lên Firebase theo HubId để FE lấy cho dễ (Thống nhất 1 ID)
                _ = _firebaseService.UpdateHubDataAsync(hub.HubId, new
                {
                    temperature = data.v1,
                    humidity = data.v2,
                    pressure = data.v3,
                    updatedAt = vietnamNow
                });

                var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                await ProcessSensorReading(sensorService, sensors, "Temperature", data.v1, hub.HubId);
                await ProcessSensorReading(sensorService, sensors, "Humidity", data.v2, hub.HubId);
                await ProcessSensorReading(sensorService, sensors, "Pressure", data.v3, hub.HubId);
                await sensorService.ProcessHubDataAsync(hub.HubId, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing MQTT message from chipId: {chipId}");
            }
            finally
            {
                _statusLock.Release();
            }
        }

        private async Task ProcessSensorReading(ISensorService sensorService, List<Sensor> sensors, string typeName, double value, int hubId)
        {
            var sensor = sensors.FirstOrDefault(s => s.Type != null && s.Type.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            if (sensor == null) return;

            await BroadcastSensorData(sensor.SensorId, value, hubId);

            if (sensor.Status != "Online")
            {
                await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Online");
                sensor.Status = "Online";
                await BroadcastSensorStatusChange(sensor.SensorId, "Online", hubId);
            }
        }

        private async Task BroadcastHubStatusChange(int hubId, bool isOnline, DateTime? lastHandshake = null)
        {
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
            await _hubContext.Clients.All.SendAsync("ReceiveSensorStatusChange", new
            {
                sensorId = sensorId,
                status = status,
                hubId = hubId,
                updatedAt = DateTime.UtcNow
            });
        }

        private async Task BroadcastHubEnvironmentData(int hubId, double temperature, double humidity, double pressure)
        {
            await _hubContext.Clients.Group($"hub_{hubId}").SendAsync("ReceiveHubEnvironmentData", new
            {
                hubId = hubId,
                temperature = temperature,
                humidity = humidity,
                pressure = pressure,
                updatedAt = DateTime.UtcNow
            });
        }

        private async Task BroadcastSensorData(int sensorId, double value, int hubId)
        {
            await _hubContext.Clients.Group($"sensor_{sensorId}").SendAsync("ReceiveSensorData", new
            {
                sensorId = sensorId,
                value = value,
                hubId = hubId,
                recordedAt = DateTime.UtcNow
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConnectToMqttAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken).ContinueWith(_ => { });
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient != null)
            {
                var disconnectOptions = new MqttClientDisconnectOptionsBuilder().Build();
                await _mqttClient.DisconnectAsync(disconnectOptions);
                _mqttClient.Dispose();
            }
            await base.StopAsync(cancellationToken);
        }
    }
}
