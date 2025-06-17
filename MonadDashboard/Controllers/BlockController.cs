using Microsoft.AspNetCore.Mvc;
using MonadDashboard.Services;

namespace MonadDashboard.Controllers;

[ApiController]
[Route("/api")]
public class BlockController : ControllerBase
{
    private readonly IRequests _requests;

    public BlockController(IRequests requests)
    {
        _requests = requests;
    }

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] int page, int pageSize = 20)
    {
        var response = await _requests.GetLatestBlockData(page, pageSize);
        
        return Ok(new
        {
            response,
            hasMore = response.Count == pageSize
        });
    }
}