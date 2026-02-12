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

        private string Broker => _configuration["MqttSettings:Broker"] ?? "mqtt1.eoh.io";
        private int Port => int.Parse(_configuration["MqttSettings:Port"] ?? "1883");
        private string GatewayToken => _configuration["MqttSettings:GatewayToken"] ?? "";
        private string TopicTemplate => _configuration["MqttSettings:TopicTemplate"] ?? "eoh/chip/{0}/third_party/+/data";

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

        private async Task ConnectToMqttAsync()
        {
            try
            {
                if (!_mqttClient.IsConnected)
                {
                    await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MQTT Broker. Retrying in 5 seconds...");
                await Task.Delay(5000);
                await ConnectToMqttAsync();
            }
        }

        private async Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            _logger.LogInformation("Connected to MQTT Broker.");
            string subscribeTopic = string.Format(TopicTemplate, GatewayToken);
            
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(subscribeTopic))
                .Build();
            
            await _mqttClient.SubscribeAsync(subscribeOptions);
            _logger.LogInformation($"Subscribed to wildcard topic: {subscribeTopic}");
        }

        private Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            _logger.LogWarning("Disconnected from MQTT Broker. Attempting to reconnect...");
            Task.Run(async () =>
            {
                await Task.Delay(5000);
                await ConnectToMqttAsync();
            });
            return Task.CompletedTask;
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
            _logger.LogInformation($"Received Message on {topic}: {payload}");

            string[] topicSegments = topic.Split('/');
            if (topicSegments.Length < 2) return;
            string chipId = topicSegments[topicSegments.Length - 2]; 

            using (var scope = _scopeFactory.CreateScope())
            {
                var sensorService = scope.ServiceProvider.GetRequiredService<ISensorService>();
                var hubService = scope.ServiceProvider.GetRequiredService<IHubService>();
                var systemLogService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

                try
                {
                    var data = JsonSerializer.Deserialize<EohWebhookDto>(payload);
                    if (data == null) return;

                    await systemLogService.LogOptionAsync("MQTT-Listener", $"Topic: {topic} | Payload: {payload}");

                    string macAddress = data.v12 ?? chipId;
                    var hub = await hubService.GetHubByMacAsync(macAddress);

                    if (hub != null)
                    {
                        // 1. Hub Status Update (Offline -> Online)
                        bool wasOffline = hub.IsOnline != true;
                        
                        hub.IsOnline = true;
                        try 
                        {
                            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                            hub.LastHandshake = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                        }
                        catch
                        {
                            hub.LastHandshake = DateTime.UtcNow.AddHours(7);
                        }

                        // Update Hub info (address etc)
                        if (!string.IsNullOrEmpty(data.v5) || !string.IsNullOrEmpty(data.v6))
                        {
                            if (hub.Site != null)
                            {
                                string newAddress = $"{data.v6}, {data.v5}".Trim(',', ' ');
                                if (!string.IsNullOrEmpty(newAddress)) hub.Site.Address = newAddress;
                            }
                        }

                        await hubService.UpdateHubAsync(hub);

                        if (wasOffline)
                        {
                            // Broadcast Hub Status Change (Requirement 2)
                            await BroadcastHubStatusChange(hub.HubId, true);
                        }

                        // 2. Hub Environment Data Update (Requirement 3: Temp, Hum, Pressure)
                        // This broadcasts to the specific hub group
                        await BroadcastHubEnvironmentData(hub.HubId, data.v1, data.v2, data.v3);


                        // 3. Process Sensor Readings & Status (Requirement 1)
                        var sensors = await sensorService.GetSensorsByHubIdAsync(hub.HubId);

                        await ProcessSensorReading(sensorService, sensors, "Temperature", data.v1, hub.HubId);
                        await ProcessSensorReading(sensorService, sensors, "Humidity", data.v2, hub.HubId);
                        await ProcessSensorReading(sensorService, sensors, "Pressure", data.v3, hub.HubId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing MQTT message from chipId: {chipId}");
                }
            }
        }

        private async Task ProcessSensorReading(ISensorService sensorService, List<Sensor> sensors, string typeName, double value, int hubId)
        {
            var sensor = sensors.FirstOrDefault(s => s.Type != null && s.Type.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            if (sensor != null)
            {
                await sensorService.ProcessReadingAsync(sensor.SensorId, (float)value);
                
                if (sensor.Status != "Online")
                {
                    await sensorService.UpdateSensorStatusAsync(sensor.SensorId, "Online");
                    sensor.Status = "Online";
                    
                    // Broadcast Sensor Status Change (Requirement 1)
                    await BroadcastSensorStatusChange(sensor.SensorId, "Online", hubId);
                }
            }
        }

        private async Task BroadcastHubStatusChange(int hubId, bool isOnline)
        {
             await _hubContext.Clients.All.SendAsync("ReceiveHubStatusChange", new
            {
                hubId = hubId,
                isOnline = isOnline,
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
            // Broadcasts to clients viewing the specific hub details
            await _hubContext.Clients.Group($"hub_{hubId}").SendAsync("ReceiveHubEnvironmentData", new
            {
                hubId = hubId,
                temperature = temperature,
                humidity = humidity,
                pressure = pressure,
                updatedAt = DateTime.UtcNow
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConnectToMqttAsync();
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
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
