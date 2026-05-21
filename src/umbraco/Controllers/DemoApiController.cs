using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UmbracoMonitoring.Configuration;

namespace UmbracoMonitoring.Controllers;

[ApiController]
[Route("umbraco/api/demo")]
public class DemoApiController : ControllerBase
{
    private readonly DemoOptions _options;
    private readonly Random _random = new();

    public DemoApiController(IOptions<DemoOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("slow")]
    public async Task<IActionResult> Slow([FromQuery] int delayMs = 100, CancellationToken cancellationToken = default)
    {
        if (!_options.SlowEndpoint)
        {
            return NotFound();
        }

        delayMs = Math.Clamp(delayMs, 50, 500);
        await Task.Delay(delayMs, cancellationToken);
        return Ok(new { delayedMs = delayMs });
    }

    [HttpGet("fault")]
    public IActionResult Fault([FromQuery] double? rate = null)
    {
        if (!_options.FaultEndpoint)
        {
            return NotFound();
        }

        double faultRate = rate ?? _options.FaultRate;
        faultRate = Math.Clamp(faultRate, 0, 1);

        if (_random.NextDouble() < faultRate)
        {
            return StatusCode(500, new { error = "Injected fault for demo." });
        }

        return Ok(new { status = "ok" });
    }
}
