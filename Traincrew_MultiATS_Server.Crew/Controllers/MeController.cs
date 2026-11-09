using Microsoft.AspNetCore.Mvc;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

[ApiController]
public class MeController(IDiscordService discordService) : ControllerBase
{
    /// <remarks>
    /// [Authorize] を付けず、FallbackPolicy(出勤台 IdP のトークン + 入鋏ロール)で保護する。
    /// 認可が有効でトークンが無ければ 401、入鋏ロールが無ければ 403 になる。
    /// 認可が無効(ローカルデバッグ)なら、トークン無しで全許可のロールを返す。
    /// </remarks>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        // sub は Discord ユーザー ID(10進数の文字列)
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
