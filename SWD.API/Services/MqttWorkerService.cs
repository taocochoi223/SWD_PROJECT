using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;
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

        private IMqttClient _mqttClient = null!;
        private MqttClientOptions _mqttOptions = null!;

        // Dùng để bỏ qua data message bị buffer sau khi device tắt
        // tránh trường hợp hub bị set ONLINE lại ngay sau khi vừa OFFLINE
        private DateTime? _lastOfflineSignalTime = null;
        private static readonly TimeSpan OfflineProtectionWindow = TimeSpan.FromSeconds(30);

        private string Broker => _configuration["MqttSettings:Broker"] ?? "mqtt1.eoh.io";
        private int Port => int.Parse(_configuration["MqttSettings:Port"] ?? "1883");
        private string GatewayToken => _configuration["MqttSettings:GatewayToken"] ?? "";
        private string TopicTemplate => _configuration["MqttSettings:TopicTemplate"] ?? "eoh/chip/{0}/third_party/+/data";
        private string StatusTopicTemplate => _configuration["MqttSettings:StatusTopicTemplate"] ?? "eoh/chip/{0}/is_online";

        public MqttWorkerService(ILogger<MqttWorkerService> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration, IHubContext<SensorHub> hubContext)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _hubContext = hubContext;
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
            string dataTopic = string.Format(TopicTemplate, GatewayToken);
            string statusTopic = string.Format(StatusTopicTemplate, GatewayToken);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(dataTopic))
                .WithTopicFilter(f => f.WithTopic(statusTopic))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);
            _logger.LogInformation($"Subscribed to data topic: {dataTopic}");
            _logger.LogInformation($"Subscribed to status topic: {statusTopic}");
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

            string statusTopic = string.Format(StatusTopicTemplate, GatewayToken);

            // Xử lý status topic (birth/close/will)
            if (topic == statusTopic)
            {
                // Bỏ qua retained message khi mới subscribe
                // Broker lưu message cũ {"ol":0} và gửi lại khi subscribe → gây false OFFLINE
                if (e.ApplicationMessage.Retain)
                {
                    _logger.LogWarning("[StatusMessage] Skipping retained message: " + payload);
                    return;
                }

                await HandleStatusMessage(payload);
                return;
            }

            // Xử lý data topic
            // Nếu đang trong protection window (30s sau khi nhận {"ol":0}),
            // bỏ qua data message bị buffer trong queue để tránh hub bị set ONLINE lại
            if (_lastOfflineSignalTime.HasValue &&
                (DateTime.UtcNow - _lastOfflineSignalTime.Value) < OfflineProtectionWindow)
            {
                _logger.LogWarning("[DataMessage] Skipping buffered message - offline protection window active.");
                return;
            }

            string[] topicSegments = topic.Split('/');
            if (topicSegments.Length < 2) return;
            string chipId = topicSegments[topicSegments.Length - 2];

            await ProcessDataMessage(chipId, payload);
        }

        private async Task HandleStatusMessage(string payload)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("ol", out var olValue)) return;

                int onlineStatus = olValue.GetInt32();
                bool isOnline = onlineStatus == 1;

                _logger.LogInformation($"[StatusMessage] Device status changed: {(isOnline ? "ONLINE" : "OFFLINE")}");

                using var scope = _scopeFactory.CreateScope();
                var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
                var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
                var systemLogService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

                await systemLogService.LogOptionAsync("MQTT-Status", $"Device {(isOnline ? "ONLINE" : "OFFLINE")} | Payload: {payload}");

                DateTime vietnamNow;
                try
                {
                    vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                }
                catch
                {
                    vietnamNow = DateTime.UtcNow.AddHours(7);
                }

                var (allHubs, _) = await hubService.GetAllHubsAsync();

                if (isOnline)
                {
                    // Xóa protection window để data message hoạt động bình thường trở lại
                    _lastOfflineSignalTime = null;

                    // Refresh LastHandshake cho TẤT CẢ hub (kể cả hub đang ONLINE)
                    // Nếu chỉ refresh hub OFFLINE thì hub ONLINE sẽ không được cập nhật
                    // → StatusMonitorService sẽ mark chúng OFFLINE sau threshold → ON-OFF liên tục
                    foreach (var hub in allHubs)
                    {
                        hub.LastHandshake = vietnamNow;
                        await hubService.UpdateHubAsync(hub);
                    }

                    // Chỉ broadcast ONLINE cho hub đang OFFLINE
                    var offlineHubs = allHubs.Where(h => h.IsOnline != true).ToList();
                    foreach (var hub in offlineHubs)
                    {
                        hub.IsOnline = true;
                        await BroadcastHubStatusChange(hub.HubId, true, hub.LastHandshake);
                        _logger.LogInformation($"[StatusMessage] Hub {hub.HubId} ({hub.Name}) → ONLINE");
                    }

                    // Set sensor ONLINE cho các hub vừa chuyển trạng thái
                    foreach (var hub in offlineHubs)
                    {
                        var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                        foreach (var sensor in sensors)
                        {
                            if (sensor.Status != "Online")
                            {
                                await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Online");
                                await BroadcastSensorStatusChange(sensor.SensorId, "Online", hub.HubId);
                            }
                        }
                    }
                }
                else
                {
                    // Bật protection window 30s để bỏ qua data message bị buffer trước khi tắt
                    _lastOfflineSignalTime = DateTime.UtcNow;
                    _logger.LogInformation($"[StatusMessage] Offline protection window started for {OfflineProtectionWindow.TotalSeconds}s.");

                    // Set tất cả hub đang ONLINE → OFFLINE
                    var onlineHubs = allHubs.Where(h => h.IsOnline == true).ToList();
                    foreach (var hub in onlineHubs)
                    {
                        hub.IsOnline = false;
                        await hubService.UpdateHubAsync(hub);
                        await BroadcastHubStatusChange(hub.HubId, false, hub.LastHandshake);
                        _logger.LogInformation($"[StatusMessage] Hub {hub.HubId} ({hub.Name}) → OFFLINE");
                    }

                    // Set sensor OFFLINE
                    foreach (var hub in onlineHubs)
                    {
                        var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                        foreach (var sensor in sensors)
                        {
                            if (sensor.Status != "Offline")
                            {
                                await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Offline");
                                await BroadcastSensorStatusChange(sensor.SensorId, "Offline", hub.HubId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing status message");
            }
        }

        private async Task ProcessDataMessage(string chipId, string payload)
        {
            using var scope = _scopeFactory.CreateScope();
            var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
            var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
            var systemLogService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

            try
            {
                var data = JsonSerializer.Deserialize<EohWebhookDto>(payload);
                if (data == null) return;

                await systemLogService.LogOptionAsync("MQTT-Listener", $"Topic chipId: {chipId} | Payload: {payload}");

                string macAddress = data.v12 ?? chipId;
                var hub = await hubService.GetHubByMacAsync(macAddress);
                if (hub == null) return;

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
                    await BroadcastHubStatusChange(hub.HubId, true, hub.LastHandshake);
                }

                await BroadcastHubEnvironmentData(hub.HubId, data.v1, data.v2, data.v3);

                var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);
                await ProcessSensorReading(sensorService, sensors, "Temperature", data.v1, hub.HubId);
                await ProcessSensorReading(sensorService, sensors, "Humidity", data.v2, hub.HubId);
                await ProcessSensorReading(sensorService, sensors, "Pressure", data.v3, hub.HubId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing MQTT message from chipId: {chipId}");
            }
        }

        private async Task ProcessSensorReading(ISensorService sensorService, List<Sensor> sensors, string typeName, double value, int hubId)
        {
            var sensor = sensors.FirstOrDefault(s => s.Type != null && s.Type.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            if (sensor == null) return;

            await sensorService.ProcessReadingAsync(sensor.SensorId, (float)value);
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
