using BillFlow.Api.Domain;
using BillFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(CustomerRole.Admin))]
public class AdminController : ControllerBase
{
    private readonly IDunningSweeper _dunningSweeper;

    public AdminController(IDunningSweeper dunningSweeper)
    {
        _dunningSweeper = dunningSweeper;
    }

    /// <summary>
    /// Manually runs the same sweep the background worker performs on a timer: renew subscriptions
    /// whose period just ended and retry invoices whose dunning schedule is due. Handy for demos
    /// and tests that don't want to wait for the timer.
    /// </summary>
    [HttpPost("dunning/run")]
    public async Task<ActionResult<DunningSweepResult>> RunDunningSweep(CancellationToken ct)
    {
        var result = await _dunningSweeper.RunAsync(ct);
        return Ok(result);
    }
}
