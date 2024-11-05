using System.Security.Claims;
using System.Text.Json;
using Discord.Net;
using Discord.Rest;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace Traincrew_MultiATS_Server.Controllers;

[Route("auth")]
public class AuthenticationController(ILogger<AuthenticationController> logger, DiscordService discordService) : ControllerBase
{
    private readonly ILogger _logger = logger;
    [HttpGet("challenge")]
    public IResult DiscordChallenge()
    {
        return Results.Challenge(null, [Providers.Discord]);
    }

    [HttpGet, HttpPost]
    [Route("callback")]
    public async Task<IResult> DiscordCallback()
    {
        var result = await HttpContext.AuthenticateAsync(Providers.Discord);
        if (!result.Succeeded)
        {
            return Results.BadRequest(); 
        }
       
        // Discordのトークンを取得
        var token = result.Properties.GetTokenValue("backchannel_access_token");
        if(token is null)
        {
            return Results.BadRequest();
        }
        
        // Traincrewサーバーにユーザーが所属しているか確認
        RestGuildUser member;
        TraincrewRole role;
        try
        {
            (member, role) = await discordService.DiscordAuthentication(token);
        }
        catch (HttpException e)
        {
            _logger.LogError(e, "Discord authentication failed.");
            return Results.BadRequest("Discord authentication failed.");
        }
        catch (DiscordAuthenticationException e)
        {
            return Results.BadRequest(e.Message);
        }
        // Build an identity based on the external claims and that will be used to create the authentication cookie.
        var identity = new ClaimsIdentity(authenticationType: "ExternalLogin");

        // By default, OpenIddict will automatically try to map the email/name and name identifier claims from
        // their standard OpenID Connect or provider-specific equivalent, if available. If needed, additional
        // claims can be resolved from the external identity and copied to the final authentication cookie.
        identity
            .SetClaim(ClaimTypes.Name, member.Username)
            .SetClaim(ClaimTypes.NameIdentifier, member.Id.ToString())
            // Preserve the registration details to be able to resolve them later.
            .SetClaim(OpenIddictConstants.Claims.Private.RegistrationId, result.Principal!.GetClaim(OpenIddictConstants.Claims.Private.RegistrationId))
            .SetClaim(OpenIddictConstants.Claims.Private.ProviderName, result.Principal!.GetClaim(OpenIddictConstants.Claims.Private.ProviderName))
            .SetClaim(TraincrewRole.ClaimType, JsonSerializer.Serialize(role));

        // Build the authentication properties based on the properties that were added when the challenge was triggered.
        var properties = new AuthenticationProperties(result.Properties.Items)
        {
            RedirectUri = result.Properties.RedirectUri ?? "/auth/success"
        };

        // Ask the default sign-in handler to return a new cookie and redirect the
        // user agent to the return URL stored in the authentication properties.
        //
        // For scenarios where the default sign-in handler configured in the ASP.NET Core
        // authentication options shouldn't be used, a specific scheme can be specified here.
        return Results.SignIn(new ClaimsPrincipal(identity), properties);
    }
    
    [HttpGet("success")]
    public Task<IResult> Success()
    {
        return Task.FromResult(Results.Ok("認証が完了しました"));
    }
    
    [HttpGet("logout")]
    public async Task<IResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Results.Ok();
    }
}