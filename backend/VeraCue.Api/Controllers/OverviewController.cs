using Microsoft.AspNetCore.Mvc;
using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Services.Calls;

namespace VeraCue.Api.Controllers;

/// <summary>GET /api/overview — numbers and recent calls for the Overview page (sign-in required).</summary>
[ApiController]
[Route("api/overview")]
public class OverviewController(CallService calls) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OverviewDto>> Get(CancellationToken ct) => Ok(await calls.OverviewAsync(ct));
}
