using System.Security.Claims;
using Discord.Net;
using Discord.Rest;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Client.AspNetCore;
using OpenIddict.Server.AspNetCore;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

[Route("auth")]
public class AuthenticationController(ILogger<AuthenticationController> logger, DiscordService discordService)
    : ControllerBase
{
    private readonly ILogger _logger = logger;


    [HttpGet, HttpPost]
    [Route("authorize")]
    public async Task<IResult> DiscordAuthorize()
    {
        // Resolve the claims stored in the cookie created after the Discord authentication dance.
        // If the principal cannot be found, trigger a new challenge to redirect the user to GitHub.
        //
        // For scenarios where the default authentication handler configured in the ASP.NET Core
        // authentication options shouldn't be used, a specific scheme can be specified here.
        var principal = (await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)).Principal;
        if (principal is null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = HttpContext.Request.GetEncodedUrl()
            };

            return Results.Challenge(properties, [Providers.Discord]);
        }

        // クッキーの認証情報を削除
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // ユーザーIDを取得
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            // ユーザーIDが取得できない場合はUnauthorizedを返す
            return Results.Forbid(
                authenticationSchemes: new List<string>{OpenIddictServerAspNetCoreDefaults.AuthenticationScheme},
                properties: new(new Dictionary<string, string?>
                {
                    [OpenIddictClientAspNetCoreConstants.Properties.Error] = 
                        Errors.ServerError,
                    [OpenIddictClientAspNetCoreConstants.Properties.ErrorDescription] = 
                        "ユーザーIDが見つかりませんでした。これが表示されるとしたらおそらくバグです。たぶん。"
                }));
        }
        // ユーザーIDからそのDiscordユーザーが運転会に参加可能か調べる
        var canAuthenticate = await discordService.IsUserCanAuthenticate(ulong.Parse(userId));
        if (!canAuthenticate)
        {
            // 運転会に参加できない場合はAccessDeniedを返す
            return Results.Forbid(
                authenticationSchemes: new List<string>{OpenIddictServerAspNetCoreDefaults.AuthenticationScheme},
                properties: new(new Dictionary<string, string?>
                {
                    [OpenIddictClientAspNetCoreConstants.Properties.Error] =
                        Errors.AccessDenied,
                    [OpenIddictClientAspNetCoreConstants.Properties.ErrorDescription] = 
                        "運転会サーバーに所属していないか、入鋏ロールがついていません。司令主任にお問い合わせください。"
                }));
        }
        
        
        // Create the claims-based identity that will be used by OpenIddict to generate tokens.
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);
        
        // ローカルクッキーから取得したクレームをOpenIddictのクレームに変換 
        identity.AddClaim(new Claim(Claims.Subject, principal.FindFirst(ClaimTypes.NameIdentifier)!.Value));
        identity.AddClaim(new Claim(Claims.Name, principal.FindFirst(ClaimTypes.Name)!.Value)
            .SetDestinations(Destinations.AccessToken));
        identity.AddClaim(new Claim(Claims.Private.RegistrationId,
            principal.FindFirst(Claims.Private.RegistrationId)!.Value)
            .SetDestinations(Destinations.AccessToken));
        identity.AddClaim(new Claim(Claims.Private.ProviderName,
            principal.FindFirst(Claims.Private.ProviderName)!.Value)
            .SetDestinations(Destinations.AccessToken));

        // Openiddictにトークンの生成とOAuth2トークンのレスポンスを返すように依頼
        return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
        var authenticationToken = result.Properties
            .GetTokens()
            .FirstOrDefault(token => token.Name is OpenIddictClientAspNetCoreConstants.Tokens.BackchannelAccessToken);
        if (authenticationToken is null)
        {
            return Results.BadRequest();
        }
        var token = authenticationToken.Value;

        // Memberを取得
        RestGuildUser member;
        try
        {
            member = await discordService.DiscordAuthentication(token);
        }
        catch (HttpException e)
        {
            _logger.LogError(e, "Discord authentication failed.");
            return Results.BadRequest("Discord authentication failed.");
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
            .SetClaim(Claims.Private.RegistrationId,
                result.Principal!.GetClaim(Claims.Private.RegistrationId))
            .SetClaim(Claims.Private.ProviderName,
                result.Principal!.GetClaim(Claims.Private.ProviderName));

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
        return Results.SignIn(new(identity), properties);
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
        return Results.Ok("ログアウトしました");
    }
}