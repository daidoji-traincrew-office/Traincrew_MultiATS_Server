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
        // 認可が有効の場合に、トークンなしでアクセスした場合、403が返される
        // この処理が実行されるのは、認可が有効でトークンあり または、認可が無効の場合(ローカルデバッグ用)
        
        // MemberIDを取得
        var memberIdString = User.FindFirst(Claims.Subject)?.Value;
        ulong? memberId = memberIdString != null ? ulong.Parse(memberIdString) : null;

        var role = await discordService.GetRoleByMemberId(memberId);
        return Ok(new
        {
            Name = User.Identity?.Name ?? "ローカルデバッグモード",
            Role = role
        });
    }
}