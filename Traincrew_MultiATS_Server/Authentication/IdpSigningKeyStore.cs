using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace Traincrew_MultiATS_Server.Authentication;

/// <summary>
/// 出勤台 IdP の署名公開鍵(<c>*.cer</c>)をディレクトリから読み込み、OpenIddict.Validation に構成として渡す。
/// OpenIddict.Validation は認証のたびに <see cref="GetConfigurationAsync"/> を呼ぶので、
/// <see cref="Reload"/> で構成を差し替えれば、再起動せずに鍵の追加・撤去が反映される。
/// </summary>
public class IdpSigningKeyStore : IConfigurationManager<OpenIddictConfiguration>
{
    private const string CertificateExtension = ".cer";

    /// <summary>
    /// kid が一致する鍵が無いときの読み直し(<see cref="RequestRefresh"/>)の最小間隔。
    /// 偽の kid を付けたリクエストで、ファイルの読み込みを連打させないため。
    /// </summary>
    public static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromMinutes(1);

    private readonly IOptions<IdpAuthenticationOptions> _options;
    private readonly ILogger<IdpSigningKeyStore> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly object _reloadLock = new();
    private volatile OpenIddictConfiguration _configuration;
    private DateTimeOffset? _lastRefreshRequestedAt;

    public IdpSigningKeyStore(
        IOptions<IdpAuthenticationOptions> options,
        ILogger<IdpSigningKeyStore> logger,
        TimeProvider timeProvider)
    {
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider;
        // 読み込むまでは鍵が 0 個なので、どのトークンも通らない(401)
        _configuration = CreateConfiguration([]);
    }

    /// <summary>
    /// 現在信頼している鍵の kid(証明書の Thumbprint)
    /// </summary>
    public IReadOnlyList<string> KeyIds => _configuration.SigningKeys.Select(key => key.KeyId).ToList();

    public Task<OpenIddictConfiguration> GetConfigurationAsync(CancellationToken cancel)
    {
        return Task.FromResult(_configuration);
    }

    /// <summary>
    /// kid が一致する鍵が見つからなかったときに OpenIddict.Validation から呼ばれる。
    /// 次の鍵が置かれた直後かもしれないので読み直す。ただし <see cref="MinimumRefreshInterval"/> に 1 回まで。
    /// </summary>
    public void RequestRefresh()
    {
        lock (_reloadLock)
        {
            var now = _timeProvider.GetUtcNow();
            if (now - _lastRefreshRequestedAt < MinimumRefreshInterval)
            {
                return;
            }

            _lastRefreshRequestedAt = now;
        }

        Reload();
    }

    /// <summary>
    /// 鍵のディレクトリを読み直す。読み込めた鍵が 0 個のときは、前回の鍵を使い続ける。
    /// </summary>
    /// <returns>読み直した後に信頼している鍵の数</returns>
    public int Reload()
    {
        lock (_reloadLock)
        {
            var directory = _options.Value.SigningKeyDirectory;
            var keys = LoadKeys(directory);
            if (keys.Count == 0)
            {
                var current = _configuration.SigningKeys.Count;
                if (current == 0)
                {
                    _logger.LogError(
                        "出勤台 IdP の署名公開鍵がディレクトリ {Directory} に 1 つもありません。すべてのトークンを拒否します。",
                        directory);
                }
                else
                {
                    _logger.LogError(
                        "出勤台 IdP の署名公開鍵がディレクトリ {Directory} に 1 つもありません。前回読み込んだ鍵 {KeyIds} を使い続けます。",
                        directory, string.Join(", ", KeyIds));
                }

                return current;
            }

            _configuration = CreateConfiguration(keys);
            _logger.LogInformation(
                "出勤台 IdP の署名公開鍵を {Count} 個読み込みました。kid: {KeyIds}",
                keys.Count, string.Join(", ", KeyIds));
            return keys.Count;
        }
    }

    private List<SecurityKey> LoadKeys(string directory)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogError("出勤台 IdP の署名公開鍵のディレクトリ {Directory} がありません。", directory);
            return [];
        }

        try
        {
            // 出勤台は一時ファイル(.{guid}.tmp)に書いてから move で置くので、*.cer だけを読めば書きかけのファイルは見えない
            return Directory.EnumerateFiles(directory, "*" + CertificateExtension)
                .Where(path => Path.GetExtension(path) == CertificateExtension)
                .Order(StringComparer.Ordinal)
                .Select(LoadKey)
                .OfType<SecurityKey>()
                .ToList();
        }
        catch (System.Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // 権限が無い・マウントの異常などでディレクトリを読めない。0 個として扱い、前回の鍵を使い続ける
            _logger.LogError(e, "出勤台 IdP の署名公開鍵のディレクトリ {Directory} を読めませんでした。", directory);
            return [];
        }
    }

    private SecurityKey? LoadKey(string path)
    {
        try
        {
            // X509SecurityKey の KeyId は証明書の Thumbprint(大文字の16進数)で、アクセストークンの header の kid と一致する。
            // 証明書(X.509)自体の期限は見ない(ValidateIssuerSigningKey=false)
            return new X509SecurityKey(new(path));
        }
        catch (System.Exception e) when (e is CryptographicException or IOException or UnauthorizedAccessException)
        {
            // 読めなかったファイルはスキップし、次の読み直しで再試行する
            _logger.LogWarning(e, "出勤台 IdP の署名公開鍵 {Path} を読み込めませんでした。", path);
            return null;
        }
    }

    private OpenIddictConfiguration CreateConfiguration(IEnumerable<SecurityKey> keys)
    {
        var configuration = new OpenIddictConfiguration
        {
            // 未設定なら null。そのときは ValidIssuers が空になり、どのトークンも issuer の検証で落ちる
            Issuer = _options.Value.TryGetIssuerUri(out var issuer) ? issuer : null
        };
        foreach (var key in keys)
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }
}
