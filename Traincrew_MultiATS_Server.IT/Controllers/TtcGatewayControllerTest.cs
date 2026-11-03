using System.Net;
using System.Security.Claims;
using System.Text;
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
using Microsoft.Extensions.Configuration;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Crew.Controllers;

namespace Traincrew_MultiATS_Server.IT.Controllers;

/// <summary>TTC gatewayの司令卓認証と固定HTTP中継を検証する。</summary>
public sealed class TtcGatewayControllerTest
{
    /// <summary>新しい指令・観測・時計APIも固定経路だけへ中継する。</summary>
    [Theory]
    [InlineData("GET", "observation")]
    [InlineData("GET", "operation-days/11111111-1111-1111-1111-111111111111/status")]
    [InlineData("POST", "operation-days/11111111-1111-1111-1111-111111111111/commands")]
    [InlineData("GET", "operation-days/11111111-1111-1111-1111-111111111111/commands/22222222-2222-2222-2222-222222222222")]
    [InlineData("GET", "operation-days/11111111-1111-1111-1111-111111111111/runtime")]
    [InlineData("POST", "operation-days/11111111-1111-1111-1111-111111111111/runtime")]
    [InlineData("PUT", "operation-days/11111111-1111-1111-1111-111111111111/route-bindings")]
    [InlineData("POST", "sessions/11111111-1111-1111-1111-111111111111/clock-preview")]
    [InlineData("GET", "sessions/11111111-1111-1111-1111-111111111111/clock-changes/22222222-2222-2222-2222-222222222222")]
    [InlineData("POST", "sessions/11111111-1111-1111-1111-111111111111/clock-changes/22222222-2222-2222-2222-222222222222/apply")]
    [InlineData("POST", "sessions/11111111-1111-1111-1111-111111111111/clock-changes/22222222-2222-2222-2222-222222222222/reconcile")]
    public async Task Gateway_ForwardsNamedOperationRoutes(string method, string path)
    {
        var probe = new GatewayProbe((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{}", Encoding.UTF8, "application/json") }));
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/ttc/" + path + "?url=http%3A%2F%2Fevil.test");
        if (method is "POST" or "PUT") request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        request.Headers.Authorization = new("Bearer", "commander");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://interlocking:8080/api/ttc/" + path, probe.Uri?.ToString());
        Assert.Equal("Bearer commander", probe.Authorization);
    }

    /// <summary>Bearerなし、無効トークン、司令権限なしをupstreamへ転送しない。</summary>
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("invalid", HttpStatusCode.Unauthorized)]
    [InlineData("no-commander", HttpStatusCode.Forbidden)]
    public async Task Gateway_RequiresCommanderAuthentication(string? token, HttpStatusCode expected)
    {
        var probe = new GatewayProbe((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/ttc/master");
        if (token is not null) request.Headers.Authorization = new("Bearer", token);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(0, probe.RequestCount);
    }

    /// <summary>GETのsessionIdだけをqueryへ転送し、認証header以外のheaderは渡さない。</summary>
    [Fact]
    public async Task Gateway_ForwardsTypedQueryAndIdentityToken()
    {
        var probe = new GatewayProbe((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"state\":\"ready\"}", Encoding.UTF8, "application/json")
        }));
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/ttc/operation-days?sessionId=9f743fed-5c0e-4aa3-86ed-7451a5c9ed78&url=http%3A%2F%2Fevil.test");
        request.Headers.Authorization = new("Bearer", "commander");
        request.Headers.Add("X-Forwarded-Host", "evil.test");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("http://interlocking:8080/api/ttc/operation-days?sessionId=9f743fed-5c0e-4aa3-86ed-7451a5c9ed78", probe.Uri?.ToString());
        Assert.Equal("Bearer commander", probe.Authorization);
        Assert.DoesNotContain("X-Forwarded-Host", probe.ForwardedHeaders);
    }

    /// <summary>POST bodyをそのまま連動側へ送り、actor欄を書き換えない。</summary>
    [Fact]
    public async Task Gateway_ForwardsDraftBodyAndUpstreamStatus()
    {
        var forwardedBody = "";
        var probe = new GatewayProbe(async (request, ct) =>
        {
            forwardedBody = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"diagramSetId\":\"00000000-0000-0000-0000-000000000001\",\"wasReplay\":false}",
                    Encoding.UTF8, "application/json")
            };
        });
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        const string body = "{\"createdBy\":\"client\",\"actorId\":\"body-actor\"}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/ttc/diagrams/drafts")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", "commander");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(body, forwardedBody);
        Assert.Equal("http://interlocking:8080/api/ttc/diagrams/drafts", probe.Uri?.ToString());
    }

    /// <summary>連動側停止時に503を返し、接続例外の詳細を応答に含めない。</summary>
    [Fact]
    public async Task Gateway_MapsUpstreamFailureToUnavailable()
    {
        var probe = new GatewayProbe((_, _) => throw new HttpRequestException("sensitive upstream connection"));
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/ttc/master");
        request.Headers.Authorization = new("Bearer", "commander");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("sensitive", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>upstream redirectは追跡せず、転送先情報を利用者へ返さない。</summary>
    [Fact]
    public async Task Gateway_DoesNotFollowUpstreamRedirect()
    {
        var probe = new GatewayProbe((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://internal.invalid/secret") }
        }));
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/ttc/me");
        request.Headers.Authorization = new("Bearer", "commander");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal(1, probe.RequestCount);
    }

    /// <summary>32MiBを超える入力はTTC serviceへ届く前に拒否する。</summary>
    [Fact]
    public async Task Gateway_RejectsOversizedRequest()
    {
        var probe = new GatewayProbe((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/ttc/diagrams/drafts")
        {
            Content = new ByteArrayContent(new byte[32 * 1024 * 1024 + 1])
        };
        request.Headers.Authorization = new("Bearer", "commander");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal(0, probe.RequestCount);
    }

    /// <summary>中継先URLに任意パスを含む設定を拒否する。</summary>
    [Fact]
    public void Gateway_RejectsBaseUrlWithPath()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TtcGateway:BaseUrl"] = "http://interlocking:8080/anything"
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(() => services.AddTtcGateway(configuration));

        Assert.Contains("TtcGateway:BaseUrl", error.Message, StringComparison.Ordinal);
    }

    /// <summary>identityはCrew内で応答し、gateway transportへ届かない。</summary>
    [Fact]
    public async Task Gateway_LeavesLocalIdentityEndpointUnchanged()
    {
        var probe = new GatewayProbe((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var server = await CreateServerAsync(probe);
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/ttc/identity");
        request.Headers.Authorization = new("Bearer", "commander");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("/api/ttc/identity", probe.Uri?.ToString() ?? "");
        Assert.Equal(0, probe.RequestCount);
    }

    private static async Task<TestServer> CreateServerAsync(GatewayProbe probe)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddControllers().AddApplicationPart(typeof(TtcGatewayController).Assembly);
                services.AddSingleton(probe);
                services.AddHttpClient("TtcGateway")
                    .ConfigurePrimaryHttpMessageHandler(provider => new ProbeHandler(provider.GetRequiredService<GatewayProbe>()));
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

    private sealed class GatewayProbe(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        public Uri? Uri { get; set; }
        public string? Authorization { get; set; }
        public HashSet<string> ForwardedHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int RequestCount { get; set; }

        public Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            foreach (var header in request.Headers) ForwardedHeaders.Add(header.Key);
            return handler(request, cancellationToken);
        }
    }

    private sealed class ProbeHandler(GatewayProbe probe) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            probe.HandleAsync(request, cancellationToken);
    }

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
        System.Text.Encodings.Web.UrlEncoder encoder)
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
