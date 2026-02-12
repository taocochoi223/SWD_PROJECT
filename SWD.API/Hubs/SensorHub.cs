using Microsoft.AspNetCore.SignalR;

namespace SWD.API.Hubs
{
    public class SensorHub : Hub
    {
        private readonly ILogger<SensorHub> _logger;

        public SensorHub(ILogger<SensorHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        // Client joins a specific Hub group to receive its Environment Data (Temp, Hum, Pressure)
        public async Task JoinHubGroup(int hubId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"hub_{hubId}");
            await Clients.Caller.SendAsync("JoinedGroup", $"Joined group hub_{hubId}");
        }

        public async Task LeaveHubGroup(int hubId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"hub_{hubId}");
            await Clients.Caller.SendAsync("LeftGroup", $"Left group hub_{hubId}");
        }
    }
}
