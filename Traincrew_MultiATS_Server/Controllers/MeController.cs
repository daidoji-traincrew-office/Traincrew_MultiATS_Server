using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Controllers;

[ApiController]
public class MeController(DiscordService _discordService): ControllerBase
{
    
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public Task<IActionResult> GetMe()
    {
        var role = JsonSerializer.Deserialize<TraincrewRole>(User.FindFirst(TraincrewRole.ClaimType)!.Value);
        return Task.FromResult<IActionResult>(Ok(new
        {
            User.Identity?.Name,
            Role = role 
        }));
    }
}