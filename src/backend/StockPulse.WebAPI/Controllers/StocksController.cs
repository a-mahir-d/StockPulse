using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPulse.WebAPI.BackgroundServices;
using StockPulse.WebAPI.Interfaces;
using StockPulse.WebAPI.Models;

namespace StockPulse.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StocksController(IStockService stockService, StockSimulatorWorker simulator) : ControllerBase
{
    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<StockTick>>> GetRecentLogs([FromQuery] int count = 100)
    {
        if (count <= 0 || count > 500)
        {
            count = 100;
        }

        var logs = await stockService.GetRecentLogsAsync(count);
        return Ok(logs);
    }

    [HttpPost("simulator/speed")]
    public IActionResult SetSimulatorSpeed([FromQuery] SimulatorSpeed speed)
    {
        if (!Enum.IsDefined(speed))
        {
            return BadRequest(new { Message = "Geçersiz simülatör hızı. (0: Stopped, 1: Slow, 3: Medium, 5: Fast)" });
        }

        simulator.ChangeSpeed(speed);

        return Ok(new
        {
            Message = $"Current simulator speed set to {speed}",
            CurrentSpeed = simulator.GetCurrentSpeed().ToString()
        });
    }

    [HttpGet("simulator/status")]
    public IActionResult GetSimulatorStatus()
    {
        return Ok(new { CurrentSpeed = simulator.GetCurrentSpeed().ToString() });
    }
}
