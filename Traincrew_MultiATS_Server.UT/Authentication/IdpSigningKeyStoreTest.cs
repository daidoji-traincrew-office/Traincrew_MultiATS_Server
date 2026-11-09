using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Traincrew_MultiATS_Server.Authentication;

namespace Traincrew_MultiATS_Server.UT.Authentication;

/// <summary>
/// 出勤台 IdP の署名公開鍵の読み込みのテスト。
/// 契約は出勤台の Docs/authentication.md の「読み込み方」。
/// </summary>
public class IdpSigningKeyStoreTest : IDisposable
{
    private const string Issuer = "https://idp.test.example/";

    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("idp-public-");
    private readonly ManualTimeProvider _timeProvider = new(DateTimeOffset.Parse("2026-11-09T00:00:00Z"));
    private readonly IdpSigningKeyStore _store;

    public IdpSigningKeyStoreTest()
    {
        var options = Options.Create(new IdpAuthenticationOptions
        {
            Issuer = Issuer,
            SigningKeyDirectory = _directory.FullName
        });
        _store = new(options, NullLogger<IdpSigningKeyStore>.Instance, _timeProvider);
    }

    public void Dispose()
    {
        _directory.Delete(true);
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "読み込む前は鍵が0個で、issuer は設定値になっていること")]
    public async Task BeforeReload_HasNoKeys()
    {
        var configuration = await _store.GetConfigurationAsync(TestContext.Current.CancellationToken);

        Assert.Empty(configuration.SigningKeys);
        Assert.Equal(Issuer, configuration.Issuer?.AbsoluteUri);
    }

    [Theory(DisplayName = "issuer が未設定・絶対 URI でなくても例外にならず、構成の issuer は null になること")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("idp.test.example")]
    public async Task InvalidIssuer_ConfigurationHasNoIssuer(string? issuer)
    {
        var options = Options.Create(new IdpAuthenticationOptions
        {
            Issuer = issuer,
            SigningKeyDirectory = _directory.FullName
        });
        var store = new IdpSigningKeyStore(options, NullLogger<IdpSigningKeyStore>.Instance, _timeProvider);

        var configuration = await store.GetConfigurationAsync(TestContext.Current.CancellationToken);

        Assert.Null(configuration.Issuer);
    }

    [Fact(DisplayName = "*.cer だけを読み、kid が証明書の Thumbprint になること")]
    public async Task Reload_ReadsOnlyCerFiles()
    {
        using var certificate = CreateCertificate();
        WriteCer(certificate);
        // 書きかけの一時ファイル・拡張子の無いファイル・拡張子違いは読まない
        using var other = CreateCertificate();
        var bytes = other.Export(X509ContentType.Cert);
        await File.WriteAllBytesAsync(Path.Combine(_directory.FullName, $".{Guid.NewGuid()}.tmp"), bytes,
            TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(_directory.FullName, "server-signing"), bytes,
            TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(_directory.FullName, "server-signing.cer.bak"), bytes,
            TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(_directory.FullName, "server-signing.crt"), bytes,
            TestContext.Current.CancellationToken);

        var count = _store.Reload();

        Assert.Equal(1, count);
        Assert.Equal([certificate.Thumbprint], _store.KeyIds);
        var configuration = await _store.GetConfigurationAsync(TestContext.Current.CancellationToken);
        Assert.Equal(certificate.Thumbprint, Assert.Single(configuration.SigningKeys).KeyId);
    }

    [Fact(DisplayName = "壊れた .cer はスキップし、他の鍵は読み込むこと")]
    public async Task Reload_SkipsBrokenCer()
    {
        using var certificate = CreateCertificate();
        WriteCer(certificate);
        await File.WriteAllTextAsync(Path.Combine(_directory.FullName, "server-signing-BROKEN.cer"), "not a certificate",
            TestContext.Current.CancellationToken);

        var count = _store.Reload();

        Assert.Equal(1, count);
        Assert.Equal([certificate.Thumbprint], _store.KeyIds);
    }

    [Fact(DisplayName = "鍵のディレクトリが空なら、鍵は0個のままであること")]
    public void Reload_EmptyDirectory_HasNoKeys()
    {
        var count = _store.Reload();

        Assert.Equal(0, count);
        Assert.Empty(_store.KeyIds);
    }

    [Fact(DisplayName = "鍵のディレクトリが無くても例外にならず、鍵は0個のままであること")]
    public void Reload_MissingDirectory_HasNoKeys()
    {
        _directory.Delete(true);
        try
        {
            var count = _store.Reload();

            Assert.Equal(0, count);
            Assert.Empty(_store.KeyIds);
        }
        finally
        {
            _directory.Create();
        }
    }

    [Fact(DisplayName = "鍵を足して読み直すと、新しい鍵も信頼すること")]
    public void Reload_AddedKey_IsTrusted()
    {
        using var current = CreateCertificate();
        WriteCer(current);
        _store.Reload();

        using var next = CreateCertificate();
        WriteCer(next);
        var count = _store.Reload();

        Assert.Equal(2, count);
        Assert.Equal(new[] { current.Thumbprint, next.Thumbprint }.Order(), _store.KeyIds.Order());
    }

    [Fact(DisplayName = "鍵を撤去して読み直すと、その鍵を信頼しなくなること")]
    public void Reload_RemovedKey_IsNotTrusted()
    {
        using var old = CreateCertificate();
        var oldPath = WriteCer(old);
        using var current = CreateCertificate();
        WriteCer(current);
        _store.Reload();

        File.Delete(oldPath);
        var count = _store.Reload();

        Assert.Equal(1, count);
        Assert.Equal([current.Thumbprint], _store.KeyIds);
    }

    [Fact(DisplayName = "読み直しで鍵が0個になったら、前回の鍵を使い続けること")]
    public void Reload_NoKeysAfterLoaded_KeepsPreviousKeys()
    {
        using var certificate = CreateCertificate();
        var path = WriteCer(certificate);
        _store.Reload();

        File.Delete(path);
        var count = _store.Reload();

        Assert.Equal(1, count);
        Assert.Equal([certificate.Thumbprint], _store.KeyIds);
    }

    [Fact(DisplayName = "ディレクトリを読めなくても例外にならず、前回の鍵を使い続けること")]
    public void Reload_UnreadableDirectory_KeepsPreviousKeys()
    {
        using var certificate = CreateCertificate();
        WriteCer(certificate);
        _store.Reload();
        var thumbprint = certificate.Thumbprint;

        WithUnreadableDirectory(() =>
        {
            var count = _store.Reload();

            Assert.Equal(1, count);
            Assert.Equal([thumbprint], _store.KeyIds);
        });
    }

    [Fact(DisplayName = "初回からディレクトリを読めなくても例外にならず、鍵は0個のままであること")]
    public void Reload_UnreadableDirectoryAtStartup_HasNoKeys()
    {
        WithUnreadableDirectory(() =>
        {
            var count = _store.Reload();

            Assert.Equal(0, count);
            Assert.Empty(_store.KeyIds);
        });
    }

    [Fact(DisplayName = "X.509 の期限が切れた証明書の鍵も読み込むこと")]
    public void Reload_ExpiredCertificate_IsLoaded()
    {
        using var expired = CreateCertificate(DateTimeOffset.UtcNow.AddYears(-2), DateTimeOffset.UtcNow.AddDays(-1));
        WriteCer(expired);

        var count = _store.Reload();

        Assert.Equal(1, count);
        Assert.Equal([expired.Thumbprint], _store.KeyIds);
    }

    [Fact(DisplayName = "RequestRefresh は読み直すが、1分以内の再要求は無視すること")]
    public void RequestRefresh_IsThrottled()
    {
        using var first = CreateCertificate();
        WriteCer(first);
        _store.RequestRefresh();
        Assert.Single(_store.KeyIds);

        // 1分以内の再要求は読み直さない
        using var second = CreateCertificate();
        WriteCer(second);
        _timeProvider.Advance(IdpSigningKeyStore.MinimumRefreshInterval - TimeSpan.FromSeconds(1));
        _store.RequestRefresh();
        Assert.Single(_store.KeyIds);

        // 1分経てば読み直す
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _store.RequestRefresh();
        Assert.Equal(2, _store.KeyIds.Count);
    }

    /// <summary>
    /// 鍵のディレクトリを読めない状態(パーミッション 000)にして <paramref name="action"/> を実行し、元に戻す。
    /// Windows と、パーミッションが効かない root での実行ではスキップする。
    /// </summary>
    private void WithUnreadableDirectory(Action action)
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Unix のパーミッションで読めないディレクトリを作るテスト");
        }
        else
        {
            var path = _directory.FullName;
            var originalMode = File.GetUnixFileMode(path);
            File.SetUnixFileMode(path, UnixFileMode.None);
            try
            {
                if (CanEnumerate(path))
                {
                    Assert.Skip("パーミッションで読めないディレクトリを作れない(root で実行している)");
                }

                action();
            }
            finally
            {
                File.SetUnixFileMode(path, originalMode);
            }
        }
    }

    private static bool CanEnumerate(string directory)
    {
        try
        {
            _ = Directory.EnumerateFiles(directory).Any();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private string WriteCer(X509Certificate2 certificate)
    {
        var path = Path.Combine(_directory.FullName, $"server-signing-{certificate.Thumbprint}.cer");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));
        return path;
    }

    private static X509Certificate2 CreateCertificate(DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test Signing", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            notBefore ?? DateTimeOffset.UtcNow.AddDays(-1),
            notAfter ?? DateTimeOffset.UtcNow.AddYears(1));
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
