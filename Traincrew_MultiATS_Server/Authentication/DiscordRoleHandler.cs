using Microsoft.AspNetCore.Authorization;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Authentication;

public class DiscordRoleHandler(DiscordService discordService) : AuthorizationHandler<DiscordRoleRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DiscordRoleRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(Claims.Subject);
        if (userIdClaim == null)
        {
            context.Fail();
            return;
        }

        var userId = ulong.Parse(userIdClaim.Value);
        var userRole = await discordService.GetRoleByMemberId(userId);

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
