using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

/// <summary>TTC連携クライアントへ司令卓と同じ認証主体を返す。</summary>
[ApiController]
[Route("api/ttc/identity")]
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "CommanderTablePolicy")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TtcIdentityController : ControllerBase
{
    /// <summary>現在のトークンのSubjectと表示名を返す。</summary>
    [HttpGet]
    public IActionResult GetIdentity()
    {
        var subject = User.FindFirst(Claims.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return Unauthorized();

        return Ok(new { Subject = subject, Name = User.Identity?.Name });
    }
}
