using Microsoft.AspNetCore.SignalR;
using MonadDashboard.Hubs;

namespace MonadDashboard.Services.Hub;

public class DataUpdateService : BackgroundService
{
    private readonly IDataProcessor _dataProcessor;
    private readonly IHubContext<DashboardHub> _hub;

    public DataUpdateService(IDataProcessor dataProcessor,
        IHubContext<DashboardHub> hub)
    {
        _dataProcessor = dataProcessor;
        _hub = hub;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _dataProcessor.UpdateDataAsync();
            
            dynamic metrics = _dataProcessor.GetMetrics();

            await _hub.Clients.All.SendAsync(HubMethods.LiveData, new
            {
                metrics
            }, stoppingToken);

            await Task.Delay(200, stoppingToken);
        }
    }
}