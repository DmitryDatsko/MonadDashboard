﻿using Microsoft.AspNetCore.Mvc;
using MonadDashboard.Services;

namespace MonadDashboard.Controllers;

[ApiController]
[Route("/api/charts")]
public class ChartController : ControllerBase
{
    private readonly IRequests _requests;
    
    public ChartController(IRequests requests)
    {
        _requests = requests;
    }

    [HttpGet("daily-network-utilization")]
    public async Task<IActionResult> DailyNetworkUtilization([FromQuery] int range = 7)
    {
        var response = await _requests.GetDailyNetworkUtilizationAsync(range);
        
        return Ok(response);
    }
    
    [HttpGet("daily-network-transaction-fee")]
    public async Task<IActionResult> DailyNetworkTransactionFee([FromQuery] int range = 7)
    {
        var response = await _requests.GetDailyNetworkTransactionFeeAsync(range);
        
        return Ok(response);
    }
    
    [HttpGet("daily-transaction-count")]
    public async Task<IActionResult> DailyTransactionCount([FromQuery] int range = 7)
    {
        var response = await _requests.GetDailyTransactionCountAsync(range);
        
        return Ok(response);
    }
}