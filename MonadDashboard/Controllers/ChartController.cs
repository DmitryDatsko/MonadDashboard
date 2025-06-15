using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using MonadDashboard.Services;

namespace MonadDashboard.Controllers;

[ApiController]
[Route("/api/charts")]
public class ChartController : ControllerBase
{
    private readonly IRequests _requests;
    private readonly IDataProcessor _dataProcessor;

    public ChartController(IRequests requests,
        IDataProcessor dataProcessor)
    {
        _requests = requests;
        _dataProcessor = dataProcessor;
    }

    [HttpGet("daily-network-utilization")]
    public async Task<IActionResult> DailyNetworkUtilization([FromQuery] int range = 7)
    {
        var response = await _requests.GetDailyNetworkUtilization(range);

        await _dataProcessor.UpdateTotalTransaction();
        
        return Ok(response);
    }
    
    [HttpGet("daily-network-transaction-fee")]
    public async Task<IActionResult> DailyNetworkTransactionFee([FromQuery] int range = 7)
    {
        var response = await _requests.GetDailyNetworkTransactionFee(range);
        
        return Ok(response);
    }
    
    [HttpGet("daily-transaction-count")]
    public async Task<IActionResult> DailyTransactionCount([FromQuery] int range = 7)
    {
        var response = await _requests.GetDailyTransactionCount(range);
        
        return Ok(response);
    }
}