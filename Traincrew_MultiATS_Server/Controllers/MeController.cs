using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
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
        // 取得できなかった場合、ローカルデバッグモード用の情報を返す
        if(memberIdString == null)
        {
            return Ok(new
            {
                Name = "ローカルデバッグモード",
                Role = new TraincrewRole
                {
                    IsDriver = true,
                    IsDriverManager = true,
                    IsConductor = true,
                    IsCommander = true,
                    IsSignalman = true,
                    IsAdministrator = true
                } 
            });
        }
        // 取得できた場合、MemberIDを取得し、DiscordServiceを使用してRoleを取得
        var memberId = ulong.Parse(memberIdString);
        var role = await discordService.GetRoleByMemberId(memberId);
        return Ok(new
        {
            User.Identity?.Name,
            Role = role
        });
    }
}