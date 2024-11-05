using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Controllers;

[ApiController]
public class MeController: ControllerBase
{
    
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMe()
    {
        var role = JsonSerializer.Deserialize<TraincrewRole>(User.FindFirst(TraincrewRole.ClaimType)!.Value);
        return Ok(new
        {
            User.Identity?.Name,
            Role = role 
        });
    }
}