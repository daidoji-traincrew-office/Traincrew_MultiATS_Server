using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

/// <summary>司令卓認証済み要求を既存の連動サービス上で動くTTC APIへ中継する。</summary>
[ApiController]
[Route("api/ttc")]
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "CommanderTablePolicy")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(32 * 1024 * 1024)]
public sealed class TtcGatewayController(IHttpClientFactory clientFactory, IConfiguration configuration) : ControllerBase
{
    private const int MaximumRequestBytes = 32 * 1024 * 1024;
    private const int MaximumResponseBytes = 16 * 1024 * 1024;

    /// <summary>TTCのDB保護状態を確認する。</summary>
    [HttpGet("healthz")]
    public Task ForwardHealth() => ForwardAsync("api/ttc/healthz");

    /// <summary>認証済み利用者を返す。</summary>
    [HttpGet("me")]
    public Task ForwardMe() => ForwardAsync("api/ttc/me");

    /// <summary>設備マスターの読取専用スナップショットを返す。</summary>
    [HttpGet("master")]
    public Task ForwardMaster() => ForwardAsync("api/ttc/master");

    /// <summary>基本ダイヤ一覧を返す。</summary>
    [HttpGet("diagrams")]
    public Task ForwardDiagrams() => ForwardAsync("api/ttc/diagrams");

    /// <summary>指定した基本ダイヤを返す。</summary>
    [HttpGet("diagrams/{id:guid}")]
    public Task ForwardDiagram(Guid id) => ForwardAsync($"api/ttc/diagrams/{id:D}");

    /// <summary>運転会一覧を返す。</summary>
    [HttpGet("sessions")]
    public Task ForwardSessions() => ForwardAsync("api/ttc/sessions");

    /// <summary>運転会を作成する。</summary>
    [HttpPost("sessions")]
    public Task CreateSession() => ForwardAsync("api/ttc/sessions", forwardBody: true);

    /// <summary>当日制御日一覧を返す。</summary>
    [HttpGet("operation-days")]
    public Task ForwardOperationDays([FromQuery] Guid? sessionId)
    {
        var query = sessionId.HasValue ? $"?sessionId={Uri.EscapeDataString(sessionId.Value.ToString("D"))}" : "";
        return ForwardAsync("api/ttc/operation-days" + query);
    }

    /// <summary>採用基本ダイヤを指定して当日制御日を作成する。</summary>
    [HttpPost("operation-days")]
    public Task CreateOperationDay() => ForwardAsync("api/ttc/operation-days", forwardBody: true);

    /// <summary>送信前の基本ダイヤ整合性を検証する。</summary>
    [HttpPost("diagrams/validate")]
    public Task ValidateDiagram() => ForwardAsync("api/ttc/diagrams/validate", forwardBody: true);

    /// <summary>基本ダイヤの下書きを保存する。</summary>
    [HttpPost("diagrams/drafts")]
    public Task SaveDiagramDraft() => ForwardAsync("api/ttc/diagrams/drafts", forwardBody: true);

    /// <summary>基本ダイヤを公開する。</summary>
    [HttpPost("diagrams/{id:guid}/publish")]
    public Task PublishDiagram(Guid id) => ForwardAsync($"api/ttc/diagrams/{id:D}/publish", forwardBody: true);

    /// <summary>固定されたTTC APIへ認証済み要求を15秒deadline付きで中継する。</summary>
    private async Task ForwardAsync(string relativePath, bool forwardBody = false)
    {
        if (forwardBody && Request.ContentLength is > MaximumRequestBytes)
        {
            Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        var tokenHeader = Request.Headers.Authorization;
        if (tokenHeader.Count != 1 || !AuthenticationHeaderValue.TryParse(tokenHeader[0], out var token) ||
            !string.Equals(token.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(token.Parameter))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!TryGetBaseUri(configuration, out var baseUri))
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            byte[]? body = null;
            if (forwardBody)
            {
                var readResult = await ReadRequestBodyAsync(Request, deadline.Token);
                if (!readResult.Success)
                {
                    Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    return;
                }
                body = readResult.Body;
            }

            var target = new Uri(baseUri, relativePath);
            using var outgoing = new HttpRequestMessage(forwardBody ? HttpMethod.Post : HttpMethod.Get, target);
            outgoing.Headers.Authorization = token;
            if (body is not null)
            {
                outgoing.Content = new ByteArrayContent(body);
                if (MediaTypeHeaderValue.TryParse(Request.ContentType, out var contentType))
                {
                    outgoing.Content.Headers.ContentType = contentType;
                }
            }

            using var client = clientFactory.CreateClient("TtcGateway");
            using var upstream = await client.SendAsync(outgoing, HttpCompletionOption.ResponseHeadersRead, deadline.Token);

            if ((int)upstream.StatusCode is >= 300 and < 400)
            {
                Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            if ((int)upstream.StatusCode >= 500)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return;
            }

            if (upstream.Content.Headers.ContentLength is > MaximumResponseBytes)
            {
                Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            var result = await ReadResponseBodyAsync(upstream, deadline.Token);
            if (!result.Success)
            {
                Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            Response.StatusCode = (int)upstream.StatusCode;
            Response.Headers.CacheControl = "no-store";
            if (upstream.Headers.WwwAuthenticate.Count > 0)
            {
                Response.Headers.WWWAuthenticate = string.Join(", ", upstream.Headers.WwwAuthenticate.Select(x => x.ToString()));
            }
            if (upstream.Content.Headers.ContentType is not null)
            {
                Response.ContentType = upstream.Content.Headers.ContentType.ToString();
            }
            await Response.Body.WriteAsync(result.Body, HttpContext.RequestAborted);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
        catch (HttpRequestException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
        catch (IOException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
    }

    /// <summary>要求の本文を上限付きで読み、15秒deadlineを適用する。</summary>
    private static async Task<(bool Success, byte[] Body)> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        var chunk = new byte[32 * 1024];
        while (true)
        {
            var read = await request.Body.ReadAsync(chunk, cancellationToken);
            if (read == 0) return (true, buffer.ToArray());
            if (buffer.Length + read > MaximumRequestBytes) return (false, []);
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
    }

    /// <summary>連動側の本文を上限付きで読み、過大応答を中継しない。</summary>
    private static async Task<(bool Success, byte[] Body)> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var chunk = new byte[32 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) return (true, buffer.ToArray());
            if (buffer.Length + read > MaximumResponseBytes) return (false, []);
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
    }

    /// <summary>転送先がorigin固定のHTTP(S) URIであることを設定から検証する。</summary>
    private static bool TryGetBaseUri(IConfiguration configuration, out Uri baseUri)
    {
        var rawValue = configuration["TtcGateway:BaseUrl"] ?? "http://interlocking:8080/";
        if (Uri.TryCreate(rawValue, UriKind.Absolute, out baseUri!) &&
            (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(baseUri.UserInfo) && string.IsNullOrEmpty(baseUri.Query) &&
            string.IsNullOrEmpty(baseUri.Fragment) && baseUri.AbsolutePath == "/")
        {
            return true;
        }

        baseUri = null!;
        return false;
    }
}

/// <summary>TTC gateway transportのclient設定をCrewへ追加する。</summary>
public static class TtcGatewayServiceCollectionExtensions
{
    /// <summary>転送先を一つの設定値に限定し、redirect/cookieを無効にしたclientを登録する。</summary>
    public static IServiceCollection AddTtcGateway(this IServiceCollection services, IConfiguration configuration)
    {
        var rawValue = configuration["TtcGateway:BaseUrl"] ?? "http://interlocking:8080/";
        if (!Uri.TryCreate(rawValue, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment) || baseUri.AbsolutePath != "/")
        {
            throw new InvalidOperationException("TtcGateway:BaseUrl must be an HTTP(S) origin without a path or credentials.");
        }

        services.AddHttpClient("TtcGateway", client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });
        return services;
    }
}
