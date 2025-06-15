using Microsoft.AspNetCore.SignalR;
using MonadDashboard.Services;

namespace MonadDashboard.Hubs;

public class DashboardHub : Hub
{
    private readonly IDataProcessor _dataProcessor;

    public DashboardHub(IDataProcessor dataProcessor)
    {
        _dataProcessor = dataProcessor;
    }
    
    public override async Task OnConnectedAsync()
    {
        dynamic metrics = _dataProcessor.GetMetrics();
        
        await Clients.Caller.SendAsync(HubMethods.OnConnected, new
        {
            metrics
        });
        
        await base.OnConnectedAsync();
    }
}