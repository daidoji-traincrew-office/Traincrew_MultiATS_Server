using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Client;
using OpenIddict.Client.WebIntegration;

namespace Traincrew_MultiATS_Server.Crew.Authentication;

/// <summary>Discordの認可応答issuer対応をOpenIddictの静的設定へ反映する。</summary>
public static class DiscordIssuerParameterSupport
{
    /// <summary>Discord登録だけで認可応答のissパラメーター対応を有効にする。</summary>
    public static IServiceCollection AddDiscordIssuerParameterSupport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.PostConfigure<OpenIddictClientOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                if (registration.ProviderType == OpenIddictClientWebIntegrationConstants.ProviderTypes.Discord &&
                    registration.Configuration is { } configuration)
                {
                    configuration.AuthorizationResponseIssParameterSupported = true;
                }
            }
        });

        return services;
    }
}
