using System.Diagnostics.CodeAnalysis;

namespace Traincrew_MultiATS_Server.Authentication;

/// <summary>
/// 出勤台 IdP が発行したアクセストークンを検証するための設定(設定セクション <c>Authentication</c>)。
/// トークンの契約は出勤台リポジトリの Docs/authentication.md を参照。
/// </summary>
public class IdpAuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// 受け付ける issuer(絶対 URI)。末尾の <c>/</c> まで完全一致で比較する。
    /// Crew は dev も prod も、出勤台 prod が発行したトークンだけを通すので、出勤台 prod の issuer を設定する。
    /// 既定値は無い。認可が有効なのに未設定なら起動しない。認可が無効で未設定なら、どのトークンも通らない。
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// 署名公開鍵(<c>*.cer</c>)を置くディレクトリ。出勤台の <c>cert/public/</c> を読み取り専用でマウントする。
    /// </summary>
    public string SigningKeyDirectory { get; set; } = "/idp-public";

    /// <summary>
    /// <see cref="Issuer"/> が絶対 URI として設定されていれば、それを返す
    /// </summary>
    public bool TryGetIssuerUri([NotNullWhen(true)] out Uri? issuer)
    {
        return Uri.TryCreate(Issuer, UriKind.Absolute, out issuer);
    }
}
