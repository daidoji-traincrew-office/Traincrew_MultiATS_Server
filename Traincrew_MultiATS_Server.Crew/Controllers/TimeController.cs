using Microsoft.AspNetCore.Mvc;
using Traincrew_MultiATS_Server.Repositories.Datetime;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeController(IDateTimeRepository dateTimeRepository) : ControllerBase
{
    [HttpPost("offset")]
    public IActionResult SetTimeOffset([FromBody] TimeOffsetRequest request)
    {
        var offset = TimeSpan.FromHours(request.OffsetHours);
        dateTimeRepository.SetTimeOffset(offset);

        return Ok(new
        {
            OffsetHours = request.OffsetHours,
            Message = "時差が設定されました"
        });
    }

    [HttpGet]
    public IActionResult GetTime()
    {
        var offset = dateTimeRepository.GetTimeOffset();
        var adjustedTime = dateTimeRepository.GetAdjustedNow();

        return Ok(new
        {
            OffsetHours = offset.TotalHours,
            CurrentTime = adjustedTime
        });
    }
}

public record TimeOffsetRequest(double OffsetHours);
