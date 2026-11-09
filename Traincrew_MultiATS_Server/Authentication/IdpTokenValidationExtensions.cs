using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Validation;
using Traincrew_MultiATS_Server.HostedService;

namespace Traincrew_MultiATS_Server.Authentication;

public static class IdpTokenValidationExtensions
{
    /// <summary>
    /// 出勤台 IdP が発行したアクセストークン(JWS、typ=at+jwt)を検証する OpenIddict.Validation を登録する。
    /// issuer と署名公開鍵は静的に渡し、ネットワークから discovery や JWKS を取りに行かない。
    /// 検証したトークンの sub は Discord ユーザー ID(10進数の文字列)。
    /// </summary>
    public static IServiceCollection AddIdpTokenValidation(this IServiceCollection services)
    {
        services.AddOptions<IdpAuthenticationOptions>()
            .BindConfiguration(IdpAuthenticationOptions.SectionName);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IdpSigningKeyStore>();

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                // トークンはヘッダー(Authorization: Bearer)とクエリ(access_token、SignalR の WebSocket 用)から受け取る。
                // OpenIddict の既定で、フォームの body の access_token も受け付ける
                options.UseAspNetCore();
                options.Configure(validationOptions =>
                {
                    var parameters = validationOptions.TokenValidationParameters;
                    // header の kid が一致する鍵だけで検証する(総当たりしない)
                    parameters.TryAllIssuerSigningKeys = false;
                    // 鍵(X.509 証明書)の期限は見ない。出勤台は切り替えが遅れると期限切れの鍵で署名を続けることがある。
                    // トークン自体の期限は OpenIddict が検証する
                    parameters.ValidateIssuerSigningKey = false;
                });
                // aud は付かないので、Audiences は登録しない(登録しなければ audience の検証は行われない)
            });

        services.AddOptions<OpenIddictValidationOptions>()
            .Configure<IdpSigningKeyStore, IOptions<IdpAuthenticationOptions>>((options, store, idpOptions) =>
            {
                // 構成(issuer と署名鍵)は IdpSigningKeyStore から受け取る。読み直しで差し替えられる
                options.ConfigurationManager = store;
                // 末尾の / まで完全一致させる(OpenIddict の既定は / の有無を区別しない)。
                // issuer が未設定(認可が無効な開発時)なら空にして、どのトークンも通さない
                options.TokenValidationParameters.ValidIssuers =
                    idpOptions.Value.Issuer is { } issuer ? [issuer] : [];
            });

        return services;
    }

    /// <summary>
    /// 認可を有効にするときに呼ぶ。issuer の設定を起動時に必須にし、署名公開鍵の読み込みと定期的な読み直しを始める。
    /// </summary>
    public static IServiceCollection AddIdpSigningKeyLoading(this IServiceCollection services)
    {
        services.AddOptions<IdpAuthenticationOptions>()
            .Validate(
                options => options.TryGetIssuerUri(out _),
                $"{IdpAuthenticationOptions.SectionName}:{nameof(IdpAuthenticationOptions.Issuer)} に、出勤台 IdP の issuer を絶対 URI で設定してください。")
            .ValidateOnStart();
        services.AddHostedService<IdpSigningKeyReloadHostedService>();
        return services;
    }
}
