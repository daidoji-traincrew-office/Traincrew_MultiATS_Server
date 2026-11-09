using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Traincrew_MultiATS_Server.Crew;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.IT.Authentication;

/// <summary>
/// 出勤台 IdP の代わりに、テスト用の署名鍵でアクセストークンを作る。
/// トークンの形は出勤台の Docs/authentication.md に合わせる(typ=at+jwt、kid=証明書の Thumbprint、aud なし)。
/// </summary>
public static class TestIdp
{
    /// <summary>Crew に設定する issuer(本物の出勤台の issuer は設定で渡すもので、リポジトリには置かない)</summary>
    public const string Issuer = "https://idp.test.example/";

    /// <summary>信頼しない issuer(出勤台 dev を想定)</summary>
    public const string OtherEnvironmentIssuer = "https://idp-dev.test.example/";

    /// <summary>入鋏・運転士</summary>
    public const ulong BeginnerDriverId = 900000000000000001UL;

    /// <summary>入鋏だけ</summary>
    public const ulong BeginnerOnlyId = 900000000000000002UL;

    /// <summary>運転士だが入鋏が無い</summary>
    public const ulong DriverWithoutBeginnerId = 900000000000000003UL;

    /// <summary>運転会サーバーのメンバーではない</summary>
    public const ulong NonMemberId = 900000000000000004UL;

    public static X509Certificate2 CreateCertificate(DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test Shukkindai Server Signing", rsa, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            notBefore ?? DateTimeOffset.UtcNow.AddDays(-1),
            notAfter ?? DateTimeOffset.UtcNow.AddYears(1));
    }

    /// <summary>
    /// 出勤台と同じく、公開鍵だけの X.509(DER)を server-signing-{Thumbprint}.cer の名前で置く
    /// </summary>
    public static string WriteCer(DirectoryInfo directory, X509Certificate2 certificate)
    {
        var path = Path.Combine(directory.FullName, $"server-signing-{certificate.Thumbprint}.cer");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));
        return path;
    }

    public static string CreateToken(
        X509Certificate2 signingCertificate,
        ulong subject = BeginnerDriverId,
        string issuer = Issuer,
        DateTime? issuedAt = null,
        string tokenType = "at+jwt")
    {
        return CreateToken(new X509SecurityKey(signingCertificate), subject, issuer, issuedAt, tokenType);
    }

    public static string CreateToken(
        SecurityKey signingKey,
        ulong subject = BeginnerDriverId,
        string issuer = Issuer,
        DateTime? issuedAt = null,
        string tokenType = "at+jwt")
    {
        var iat = issuedAt ?? DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            IssuedAt = iat,
            Expires = iat.AddHours(1),
            TokenType = tokenType,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject.ToString(),
                ["name"] = "alice",
                ["scope"] = "offline_access",
                ["jti"] = Guid.NewGuid().ToString(),
                ["client_id"] = "Tatehama_Shukkindai_Client"
            },
            SigningCredentials = new(signingKey, SecurityAlgorithms.RsaSha256)
        };
        // 出勤台のトークンには nbf が付かないので、既定の時刻は付けない
        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return handler.CreateToken(descriptor);
    }

    /// <summary>
    /// Crew を立てる(既定は認可あり)。鍵は <paramref name="keyDirectory"/> から読む。
    /// <paramref name="issuer"/> が null なら issuer を設定しない。
    /// DB の初期化と Discord Bot は起動しない(認証・認可だけを見るので DB にも Discord にも触れない)。
    /// </summary>
    public static WebApplicationFactory<Program> CreateFactory(
        DirectoryInfo keyDirectory,
        string? issuer = Issuer,
        bool enableAuthorization = true)
    {
        var discordService = CreateDiscordServiceMock();
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("EnableAuthorization", enableAuthorization ? "true" : "false");
            builder.UseSetting("Authentication:SigningKeyDirectory", keyDirectory.FullName);
            if (issuer != null)
            {
                builder.UseSetting("Authentication:Issuer", issuer);
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDiscordService>();
                services.AddSingleton(discordService.Object);
                var excludedHostedServices = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                                         && (descriptor.ImplementationType == typeof(InitDbHostedService)
                                             || descriptor.ImplementationType == typeof(DiscordBotHostedService)))
                    .ToList();
                excludedHostedServices.ForEach(descriptor => services.Remove(descriptor));
            });
        });
    }

    private static Mock<IDiscordService> CreateDiscordServiceMock()
    {
        var mock = new Mock<IDiscordService>();
        mock.Setup(s => s.GetRoleByMemberId(BeginnerDriverId))
            .ReturnsAsync(CreateRole(isBeginner: true, isDriver: true));
        mock.Setup(s => s.GetRoleByMemberId(BeginnerOnlyId))
            .ReturnsAsync(CreateRole(isBeginner: true, isDriver: false));
        mock.Setup(s => s.GetRoleByMemberId(DriverWithoutBeginnerId))
            .ReturnsAsync(CreateRole(isBeginner: false, isDriver: true));
        mock.Setup(s => s.GetRoleByMemberId(NonMemberId))
            .ThrowsAsync(new DiscordAuthenticationException($"Member ID: {NonMemberId} not found."));
        return mock;
    }

    private static TraincrewRole CreateRole(bool isBeginner, bool isDriver) => new()
    {
        IsBeginner = isBeginner,
        IsDriver = isDriver,
        IsDriverManager = false,
        IsConductor = false,
        IsCommander = false,
        IsSignalman = false,
        IsAdministrator = false
    };
}
