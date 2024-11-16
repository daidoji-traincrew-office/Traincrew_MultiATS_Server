using Microsoft.AspNetCore.Mvc;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Controllers;

[ApiController]
[Route("api/station")]
public class StationController(ILogger<StationController> logger, StationService stationService)
    : ControllerBase
{
    
    // Todo: これはAsyncにしよう(確信)
    [HttpGet("{name}")]
    public async Task<IActionResult> GetStation(string name)
    {
        var station = await stationService.GetStationByName(name);
        if (station == null)
        {
            return NotFound();
        }
        return Ok(station);
    }
    
}