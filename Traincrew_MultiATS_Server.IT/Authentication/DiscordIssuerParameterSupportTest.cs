using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIddict.Client.WebIntegration;
using Traincrew_MultiATS_Server.Crew.Authentication;

namespace Traincrew_MultiATS_Server.IT.Authentication;

/// <summary>Discord issuerメタデータ補正とOpenIddict標準のiss検証を確認する。</summary>
public sealed class DiscordIssuerParameterSupportTest
{
    /// <summary>AddDiscord登録のiss対応を有効化し、既存endpointを保持する。</summary>
    [Fact]
    public void AddDiscordIssuerParameterSupport_EnablesFlagAndPreservesEndpoints()
    {
        var services = new ServiceCollection();
        services.AddOpenIddict()
            .AddClient(options =>
            {
                options.AllowAuthorizationCodeFlow();
                options.AddEphemeralEncryptionKey();
                options.AddEphemeralSigningKey();
                options.UseWebProviders().AddDiscord(discord =>
                {
                    discord
                        .SetClientId("test-client")
                        .SetClientSecret("test-secret")
                        .SetRedirectUri("auth/callback");
                });
            });
        services.AddDiscordIssuerParameterSupport();

        using var provider = services.BuildServiceProvider();
        var registration = Assert.Single(provider.GetRequiredService<IOptions<OpenIddictClientOptions>>().Value.Registrations);
        var configuration = Assert.IsType<OpenIddictConfiguration>(registration.Configuration);

        Assert.True(configuration.AuthorizationResponseIssParameterSupported);
        Assert.Equal("https://discord.com/oauth2/authorize", configuration.AuthorizationEndpoint?.AbsoluteUri);
        Assert.Equal("https://discord.com/api/oauth2/token", configuration.TokenEndpoint?.AbsoluteUri);
        Assert.Equal("https://discord.com/api/oauth2/@me", configuration.UserinfoEndpoint?.AbsoluteUri);
    }

    /// <summary>OpenIddict標準handlerでDiscord issuerの一致と不一致、欠落を検証する。</summary>
    [Theory]
    [InlineData("https://discord.com", false)]
    [InlineData("https://discord.com/", false)]
    [InlineData("https://attacker.example", true)]
    [InlineData(null, true)]
    public async Task ValidateIssuerParameter_UsesSupportedDiscordIssuer(string? issuer, bool expectedRejection)
    {
        var transaction = new OpenIddictClientTransaction
        {
            Configuration = new OpenIddictConfiguration
            {
                AuthorizationResponseIssParameterSupported = true
            },
            Registration = new OpenIddictClientRegistration
            {
                Issuer = new Uri("https://discord.com/")
            },
            Request = new OpenIddictRequest(),
            EndpointType = OpenIddictClientEndpointType.Redirection
        };
        transaction.Request[OpenIddictConstants.Parameters.Iss] = issuer;
        var context = new OpenIddictClientEvents.ProcessAuthenticationContext(transaction);

        await new OpenIddictClientHandlers.ValidateIssuerParameter().HandleAsync(context);

        Assert.Equal(expectedRejection, context.IsRejected);
        if (expectedRejection)
        {
            Assert.Equal(OpenIddictConstants.Errors.InvalidRequest, context.Error);
        }
    }
}
