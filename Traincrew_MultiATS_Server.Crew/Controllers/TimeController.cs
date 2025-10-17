using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Repositories.Datetime;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeController(IDateTimeRepository dateTimeRepository) : ControllerBase
{
    [HttpPost("offset")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult SetTimeOffset([FromBody] TimeOffsetRequest request)
    {
        var offset = TimeSpan.FromHours(request.OffsetHours);
        dateTimeRepository.SetTimeOffset(offset);

        return Ok(new
        {
            Message = "時差が設定されました",
            OffsetHours = request.OffsetHours,
            CurrentTime = dateTimeRepository.GetNow()
        });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult GetTime()
    {
        var offset = dateTimeRepository.GetTimeOffset();
        var currentTime = dateTimeRepository.GetNow();

        return Ok(new
        {
            OffsetHours = offset.TotalHours,
            CurrentTime = currentTime
        });
    }
}

public record TimeOffsetRequest(double OffsetHours);
