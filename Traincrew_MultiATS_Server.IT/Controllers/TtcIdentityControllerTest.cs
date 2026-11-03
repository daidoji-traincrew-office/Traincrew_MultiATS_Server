using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Crew.Controllers;
using Traincrew_MultiATS_Server.Hubs;

namespace Traincrew_MultiATS_Server.IT.Controllers;

/// <summary>TTC identity APIの司令卓認証、応答、キャッシュ制御を検証する。</summary>
public sealed class TtcIdentityControllerTest
{
    /// <summary>未認証、司令権限なし、許可済みの各応答を確認する。</summary>
    [Fact]
    public async Task IdentityEndpoint_UsesCommanderAuthenticationAndDisablesCaching()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/ttc/identity", cancellationToken)).StatusCode);

        using var deniedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/ttc/identity");
        deniedRequest.Headers.Authorization = new("Bearer", "no-commander");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(deniedRequest, cancellationToken)).StatusCode);

        using var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/ttc/identity");
        allowedRequest.Headers.Authorization = new("Bearer", "commander");
        using var response = await client.SendAsync(allowedRequest, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IdentityResponse>(cancellationToken);
        Assert.NotNull(payload);
        Assert.Equal("12345", payload.Subject);
        Assert.Equal("Commander Name", payload.Name);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);

        var controllerAuthorization = Assert.Single(typeof(TtcIdentityController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        var hubAuthorization = Assert.Single(typeof(CommanderTableHub).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(hubAuthorization.Policy, controllerAuthorization.Policy);
        Assert.Equal(hubAuthorization.AuthenticationSchemes, controllerAuthorization.AuthenticationSchemes);
    }

    private static async Task<TestServer> CreateServerAsync()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddControllers().AddApplicationPart(typeof(TtcIdentityController).Assembly);
                services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestBearerHandler>(
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, _ => { });
                services.AddAuthorization(options => options.AddPolicy("CommanderTablePolicy", policy =>
                    policy.Requirements.Add(new CommanderRequirement())));
                services.AddSingleton<IAuthorizationHandler, CommanderHandler>();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });
        var server = new TestServer(builder);
        await server.Host.StartAsync();
        return server;
    }

    private sealed record IdentityResponse(string Subject, string? Name);

    private sealed class CommanderRequirement : IAuthorizationRequirement;

    private sealed class CommanderHandler : AuthorizationHandler<CommanderRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CommanderRequirement requirement)
        {
            if (context.User.HasClaim("commander", "true")) context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    private sealed class TestBearerHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = Request.Headers.Authorization.ToString();
            if (token != "Bearer commander" && token != "Bearer no-commander") return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "Commander Name"),
                new(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject, "12345")
            };
            if (token == "Bearer commander") claims.Add(new("commander", "true"));
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
