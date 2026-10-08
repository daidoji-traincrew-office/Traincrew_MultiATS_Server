using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIddict.Client.WebIntegration;

namespace Traincrew_MultiATS_Server.UT.Authentication;

/// <summary>
/// Crew/Program.cs のDiscord iss回避ハンドラ(openiddict-core#2562の暫定対応)が前提とするOpenIddictの挙動を固定する。
/// このテストが落ちたら、上流が修正された(回避ハンドラを削除できる)か、ハンドラの前提が変わった合図。
/// </summary>
public class DiscordIssParameterWorkaroundTest
{
    private static async Task<OpenIddictConfiguration> GetDiscordConfigurationAsync()
    {
        var services = new ServiceCollection();
        services.AddOpenIddict().AddClient(options =>
        {
            options.AllowAuthorizationCodeFlow();
            options.AddEphemeralEncryptionKey().AddEphemeralSigningKey();
            options.UseWebProviders().AddDiscord(discord => discord
                .SetClientId("id").SetClientSecret("secret").SetRedirectUri("auth/callback"));
        });
        await using var provider = services.BuildServiceProvider();
        var all = provider.GetRequiredService<IOptionsMonitor<OpenIddictClientOptions>>().CurrentValue.Registrations;
        var registration = all.SingleOrDefault(r => r.ProviderName == OpenIddictClientWebIntegrationConstants.Providers.Discord)
            ?? throw new InvalidOperationException("regs: " + string.Join(",", all.Select(r => r.ProviderType + "/" + r.ProviderName)));
        return await registration.ConfigurationManager.GetConfigurationAsync(default);
    }

    [Fact]
    public async Task DiscordStaticConfiguration_DoesNotDeclareIssParameterSupport()
    {
        var configuration = await GetDiscordConfigurationAsync();
        Assert.NotEqual(true, configuration.AuthorizationResponseIssParameterSupported);
    }

    [Fact]
    public async Task DiscordStaticConfiguration_IssParameterSupportIsWritable()
    {
        // 回避ハンドラと同じ書き込みができ、書いた値が読めること
        var configuration = await GetDiscordConfigurationAsync();
        configuration.AuthorizationResponseIssParameterSupported = true;
        Assert.Equal(true, configuration.AuthorizationResponseIssParameterSupported);
    }

    [Fact]
    public void ValidateIssuerParameter_HandlerDescriptorExists()
    {
        // 回避ハンドラは Order - 1 でこのハンドラの直前に差し込む
        Assert.NotNull(OpenIddictClientHandlers.ValidateIssuerParameter.Descriptor);
    }
}
