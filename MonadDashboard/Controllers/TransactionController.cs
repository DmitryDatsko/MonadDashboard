using Microsoft.AspNetCore.Mvc;
using MonadDashboard.Services;

namespace MonadDashboard.Controllers;

[ApiController]
[Route("/api")]
public class TransactionController : ControllerBase
{
    private readonly IRequests _requests;

    public TransactionController(IRequests requests)
    {
        _requests = requests;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page, int pageSize = 20)
    {
        var response = await _requests.GetLatestBlockTransaction(page, pageSize);
        
        return Ok(response);
    }
}