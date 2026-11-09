using Microsoft.AspNetCore.Authorization;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Authentication;

public class DiscordRoleHandler(IDiscordService discordService) : AuthorizationHandler<DiscordRoleRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DiscordRoleRequirement requirement)
    {
        // sub は Discord ユーザー ID(10進数の文字列)。出勤台 IdP がそう発行する契約で、Hub もすべて ulong に変換して使う
        var userIdClaim = context.User.FindFirst(Claims.Subject);
        if (!ulong.TryParse(userIdClaim?.Value, out var userId))
        {
            context.Fail();
            return;
        }

        TraincrewRole userRole;
        try
        {
            userRole = await discordService.GetRoleByMemberId(userId);
        }
        catch (DiscordAuthenticationException)
        {
            // 運転会サーバーのメンバーではない(未参加・トークン発行後に退会した)。403 にする
            context.Fail();
            return;
        }

        if (requirement.Condition(userRole))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
