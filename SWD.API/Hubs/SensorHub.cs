using Microsoft.AspNetCore.SignalR;

namespace SWD.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time sensor data broadcasting
    /// </summary>
    public class SensorHub : Hub
    {
        private readonly ILogger<SensorHub> _logger;

        public SensorHub(ILogger<SensorHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a hub group to receive updates for a specific hub
        /// </summary>
        /// <param name="hubId">Hub ID to subscribe to</param>
        public async Task JoinHubGroup(int hubId)
        {
            string groupName = $"hub_{hubId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} joined group {groupName}");
            
            await Clients.Caller.SendAsync("JoinedGroup", new 
            { 
                hubId = hubId,
                groupName = groupName,
                message = $"Successfully joined hub {hubId} updates"
            });
        }

        /// <summary>
        /// Leave a hub group to stop receiving updates
        /// </summary>
        /// <param name="hubId">Hub ID to unsubscribe from</param>
        public async Task LeaveHubGroup(int hubId)
        {
            string groupName = $"hub_{hubId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} left group {groupName}");
            
            await Clients.Caller.SendAsync("LeftGroup", new 
            { 
                hubId = hubId,
                groupName = groupName,
                message = $"Left hub {hubId} updates"
            });
        }

        /// <summary>
        /// Get current connection info
        /// </summary>
        public async Task GetConnectionInfo()
        {
            await Clients.Caller.SendAsync("ConnectionInfo", new
            {
                connectionId = Context.ConnectionId,
                connectedAt = DateTime.UtcNow
            });
        }
    }
}
