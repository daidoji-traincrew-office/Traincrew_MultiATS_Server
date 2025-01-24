using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Controllers;

[ApiController]
public class MeController(DiscordService discordService) : ControllerBase
{
    
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetMe()
    {
        // Todo: memberIDこれであってる？
        var memberId = ulong.Parse(User.FindFirst(Claims.Subject)!.Value);
        var role = await discordService.GetRoleByMemberId(memberId);
        return Ok(new
        {
            User.Identity?.Name,
            Role = role
        });
    }
}