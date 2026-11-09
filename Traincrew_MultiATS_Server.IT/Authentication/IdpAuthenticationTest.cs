using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Crew;

namespace Traincrew_MultiATS_Server.IT.Authentication;

/// <summary>
/// 鍵を固定した Crew(認可あり)を、テストクラスで共有する
/// </summary>
public sealed class IdpAuthenticationFixture : IAsyncDisposable
{
    public DirectoryInfo KeyDirectory { get; } = Directory.CreateTempSubdirectory("idp-public-");

    /// <summary>信頼している鍵</summary>
    public X509Certificate2 TrustedCertificate { get; } = TestIdp.CreateCertificate();

    /// <summary>信頼しているが、X.509 の期限が切れている鍵</summary>
    public X509Certificate2 ExpiredCertificate { get; } =
        TestIdp.CreateCertificate(DateTimeOffset.UtcNow.AddYears(-2), DateTimeOffset.UtcNow.AddDays(-1));

    public WebApplicationFactory<Program> Factory { get; }

    public IdpAuthenticationFixture()
    {
        TestIdp.WriteCer(KeyDirectory, TrustedCertificate);
        TestIdp.WriteCer(KeyDirectory, ExpiredCertificate);
        Factory = TestIdp.CreateFactory(KeyDirectory);
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        TrustedCertificate.Dispose();
        ExpiredCertificate.Dispose();
        KeyDirectory.Delete(true);
    }
}

/// <summary>
/// 出勤台 IdP のアクセストークンによる認証と、入鋏ロールによる認可の IT。
/// 未認証は 401、認証済みでもロールが足りなければ 403 になる。
/// </summary>
public class IdpAuthenticationTest(IdpAuthenticationFixture fixture) : IClassFixture<IdpAuthenticationFixture>
{
    private const string MePath = "/me";
    private const string TrainHubNegotiatePath = "/hub/train/negotiate?negotiateVersion=1";

    [Fact(DisplayName = "トークンが無ければ 401")]
    public async Task NoToken_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(fixture.Factory, null));
    }

    [Fact(DisplayName = "入鋏ロールがあれば 200。aud が無くても通り、name がユーザー名になる")]
    public async Task Beginner_Returns200()
    {
        var token = TestIdp.CreateToken(fixture.TrustedCertificate, TestIdp.BeginnerOnlyId);
        using var client = fixture.Factory.CreateClient();
        using var request = CreateMeRequest(token);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"name\":\"alice\"", body);
        Assert.Contains("\"isBeginner\":true", body);
    }

    [Fact(DisplayName = "信頼している kid を名乗っていても、別の鍵で署名されていれば 401")]
    public async Task ForgedSignature_Returns401()
    {
        using var rsa = RSA.Create(2048);
        var forgedKey = new RsaSecurityKey(rsa) { KeyId = fixture.TrustedCertificate.Thumbprint };
        var token = TestIdp.CreateToken(forgedKey);

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(fixture.Factory, token));
    }

    [Theory(DisplayName = "issuer が設定値と完全一致しなければ 401")]
    [InlineData(TestIdp.OtherEnvironmentIssuer)]
    [InlineData("https://idp.test.example")]
    [InlineData("https://example.com/")]
    public async Task OtherIssuer_Returns401(string issuer)
    {
        var token = TestIdp.CreateToken(fixture.TrustedCertificate, issuer: issuer);

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(fixture.Factory, token));
    }

    [Fact(DisplayName = "トークンの期限が切れていれば 401")]
    public async Task ExpiredToken_Returns401()
    {
        var token = TestIdp.CreateToken(fixture.TrustedCertificate, issuedAt: DateTime.UtcNow.AddHours(-2));

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(fixture.Factory, token));
    }

    [Fact(DisplayName = "kid に一致する鍵が無ければ 401")]
    public async Task UnknownKid_Returns401()
    {
        using var unknown = TestIdp.CreateCertificate();
        var token = TestIdp.CreateToken(unknown);

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(fixture.Factory, token));
    }

    [Fact(DisplayName = "信頼している鍵で署名されていても、kid が一致しなければ 401(鍵を総当たりしない)")]
    public async Task TrustedKeyWithWrongKid_Returns401()
    {
        var key = new RsaSecurityKey(fixture.TrustedCertificate.GetRSAPrivateKey()) { KeyId = "UNKNOWN-KID" };
        var token = TestIdp.CreateToken(key);

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(fixture.Factory, token));
    }

    [Fact(DisplayName = "typ が at+jwt でなければ 401")]
    public async Task NotAccessTokenType_Returns401()
    {
        var token = TestIdp.CreateToken(fixture.TrustedCertificate, tokenType: "JWT");

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(fixture.Factory, token));
    }

    [Fact(DisplayName = "運転会サーバーのメンバーでなければ 403")]
    public async Task NonMember_Returns403()
    {
        var token = TestIdp.CreateToken(fixture.TrustedCertificate, TestIdp.NonMemberId);

        Assert.Equal(HttpStatusCode.Forbidden, await GetMeAsync(fixture.Factory, token));
    }

    [Fact(DisplayName = "入鋏ロールが無ければ 403")]
    public async Task WithoutBeginner_Returns403()
    {
        var token = TestIdp.CreateToken(fixture.TrustedCertificate, TestIdp.DriverWithoutBeginnerId);

        Assert.Equal(HttpStatusCode.Forbidden, await GetMeAsync(fixture.Factory, token));
    }

    [Fact(DisplayName = "X.509 の期限が切れた鍵でも、署名とトークンの期限が正しければ 200")]
    public async Task ExpiredCertificateKey_Returns200()
    {
        var token = TestIdp.CreateToken(fixture.ExpiredCertificate);

        Assert.Equal(HttpStatusCode.OK, await GetMeAsync(fixture.Factory, token));
    }

    [Theory(DisplayName = "Hub は入鋏と Hub ごとのロールの両方を要求し、トークンはクエリでも受け取る")]
    [InlineData(TestIdp.BeginnerDriverId, HttpStatusCode.OK)]
    [InlineData(TestIdp.BeginnerOnlyId, HttpStatusCode.Forbidden)]
    [InlineData(TestIdp.DriverWithoutBeginnerId, HttpStatusCode.Forbidden)]
    [InlineData(TestIdp.NonMemberId, HttpStatusCode.Forbidden)]
    public async Task TrainHubNegotiate_WithQueryToken(ulong subject, HttpStatusCode expected)
    {
        var token = TestIdp.CreateToken(fixture.TrustedCertificate, subject);
        using var client = fixture.Factory.CreateClient();

        using var response = await client.PostAsync(
            $"{TrainHubNegotiatePath}&access_token={Uri.EscapeDataString(token)}", null,
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact(DisplayName = "Hub もトークンが無ければ 401")]
    public async Task TrainHubNegotiate_NoToken_Returns401()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.PostAsync(TrainHubNegotiatePath, null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "認可が有効なら、存在しないパスは 404 ではなく 401(FallbackPolicy が効く)")]
    public async Task UnknownPath_Returns401()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/no-such-path", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    internal static async Task<HttpStatusCode> GetMeAsync(WebApplicationFactory<Program> factory, string? token)
    {
        using var client = factory.CreateClient();
        using var request = CreateMeRequest(token);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private static HttpRequestMessage CreateMeRequest(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        if (token != null)
        {
            request.Headers.Authorization = new("Bearer", token);
        }

        return request;
    }
}

/// <summary>
/// 鍵のディレクトリの中身を変えるテスト。テストごとに Crew を立てる。
/// </summary>
public class IdpSigningKeyReloadTest : IDisposable
{
    private readonly DirectoryInfo _keyDirectory = Directory.CreateTempSubdirectory("idp-public-");

    public void Dispose()
    {
        _keyDirectory.Delete(true);
        GC.SuppressFinalize(this);
    }

    [Theory(DisplayName = "認可が有効なのに issuer が未設定・絶対 URI でなければ、起動しない")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("idp.test.example")]
    public void MissingIssuer_FailsToStart(string? issuer)
    {
        using var factory = TestIdp.CreateFactory(_keyDirectory, issuer);

        var exception = Assert.ThrowsAny<System.Exception>(() => factory.Server);

        Assert.Contains("Authentication:Issuer", exception.ToString());
    }

    [Fact(DisplayName = "認可が無効なら、トークン無しで /me が 200、存在しないパスは 404 のまま")]
    public async Task AuthorizationDisabled_KeepsDevelopmentBehavior()
    {
        await using var factory = TestIdp.CreateFactory(_keyDirectory, issuer: null, enableAuthorization: false);
        using var client = factory.CreateClient();

        using var me = await client.GetAsync("/me", TestContext.Current.CancellationToken);
        using var unknown = await client.GetAsync("/no-such-path", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact(DisplayName = "鍵のディレクトリが空なら、どのトークンも 401")]
    public async Task EmptyDirectory_Returns401()
    {
        using var certificate = TestIdp.CreateCertificate();
        await using var factory = TestIdp.CreateFactory(_keyDirectory);

        var token = TestIdp.CreateToken(certificate);

        Assert.Equal(HttpStatusCode.Unauthorized, await IdpAuthenticationTest.GetMeAsync(factory, token));
    }

    [Fact(DisplayName = "鍵を足して読み直すと新しい kid が通り、鍵が0個になっても前の鍵で通り続ける")]
    public async Task Reload_AddsKeys_AndKeepsKeysWhenEmpty()
    {
        using var current = TestIdp.CreateCertificate();
        var currentPath = TestIdp.WriteCer(_keyDirectory, current);
        await using var factory = TestIdp.CreateFactory(_keyDirectory);
        var store = factory.Services.GetRequiredService<IdpSigningKeyStore>();
        var currentToken = TestIdp.CreateToken(current);
        Assert.Equal(HttpStatusCode.OK, await IdpAuthenticationTest.GetMeAsync(factory, currentToken));

        // 次の鍵を置いて読み直す
        using var next = TestIdp.CreateCertificate();
        var nextPath = TestIdp.WriteCer(_keyDirectory, next);
        store.Reload();
        var nextToken = TestIdp.CreateToken(next);
        Assert.Equal(HttpStatusCode.OK, await IdpAuthenticationTest.GetMeAsync(factory, nextToken));

        // マウントが外れたなどで 0 個になっても、前回の鍵を使い続ける
        File.Delete(currentPath);
        File.Delete(nextPath);
        store.Reload();
        Assert.Equal(HttpStatusCode.OK, await IdpAuthenticationTest.GetMeAsync(factory, currentToken));
        Assert.Equal(HttpStatusCode.OK, await IdpAuthenticationTest.GetMeAsync(factory, nextToken));
    }

    [Fact(DisplayName = "読み込む前の kid のトークンが届くと読み直し、次のリクエストから通る")]
    public async Task UnknownKid_TriggersRefresh()
    {
        using var current = TestIdp.CreateCertificate();
        TestIdp.WriteCer(_keyDirectory, current);
        await using var factory = TestIdp.CreateFactory(_keyDirectory);
        Assert.Equal(HttpStatusCode.OK,
            await IdpAuthenticationTest.GetMeAsync(factory, TestIdp.CreateToken(current)));

        using var next = TestIdp.CreateCertificate();
        TestIdp.WriteCer(_keyDirectory, next);
        var nextToken = TestIdp.CreateToken(next);

        // このリクエストは kid が見つからず 401 になるが、読み直しのきっかけになる
        Assert.Equal(HttpStatusCode.Unauthorized, await IdpAuthenticationTest.GetMeAsync(factory, nextToken));
        Assert.Equal(HttpStatusCode.OK, await IdpAuthenticationTest.GetMeAsync(factory, nextToken));
    }
}
