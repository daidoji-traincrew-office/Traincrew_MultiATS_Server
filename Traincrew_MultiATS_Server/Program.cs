using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenIddict.Abstractions;
using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.Discord;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Lock;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;
using Traincrew_MultiATS_Server.Repositories.Protection;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);
var enableAuthorization =
    !builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableAuthorization");
// Logging
builder.Services.AddHttpLogging(options => { options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders; });
// Proxied headers
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        var proxyIp = builder.Configuration["ProxyIP"];
        if (IPAddress.TryParse(proxyIp, out var ip))
        {
            options.KnownProxies.Add(ip);
        }
    });
}

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.MapEnum<LockType>();
dataSourceBuilder.MapEnum<NR>();
dataSourceBuilder.MapEnum<NRC>();
dataSourceBuilder.MapEnum<LCR>();
dataSourceBuilder.MapEnum<ObjectType>();
dataSourceBuilder.MapEnum<SignalIndication>();
dataSourceBuilder.MapEnum<LockConditionType>();
dataSourceBuilder.MapEnum<LeverType>();
dataSourceBuilder.MapEnum<RouteType>();
dataSourceBuilder.MapEnum<RaiseDrop>();
dataSourceBuilder.MapEnum<OperationNotificationType>();
dataSourceBuilder.MapEnum<LR>();
dataSourceBuilder.MapEnum<TtcWindowType>();
dataSourceBuilder.MapEnum<TtcWindowLinkType>();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(dataSource);
    // Todo: セッションであることを考えると、Redisを使ったほうが良いかも
    options.UseOpenIddict();
});
// SignalR
builder.Services.AddSignalR();

// 認可周り
// Cookie認可の設定
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { options.ExpireTimeSpan = TimeSpan.FromMinutes(10); });
// OpenIddictの設定
var openIddictBuilder = builder.Services.AddOpenIddict()
    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Authorizationとtokenエンドポイントを有効にする
        options.SetAuthorizationEndpointUris("auth/authorize")
            .SetTokenEndpointUris("token");

        // AuthorizationCodeFlowとRefreshTokenFlowを有効にする
        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow();

        // Register the signing and encryption credentials
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        else
        {
            // Generate a certificate at startup and register it.
            const string encryptionCertificatePath = "cert/server-encryption-certificate.pfx";
            const string signingCertificatePath = "cert/server-signing-certificate.pfx";
            EnsureCertificateExists(
                encryptionCertificatePath,
                "CN=Fabrikam Server Encryption Certificate",
                X509KeyUsageFlags.KeyEncipherment);
            EnsureCertificateExists(
                signingCertificatePath,
                "CN=Fabrikam Server Signing Certificate",
                X509KeyUsageFlags.DigitalSignature);
            options.AddEncryptionCertificate(
                new X509Certificate2(encryptionCertificatePath, string.Empty));
            options.AddSigningCertificate(
                new X509Certificate2(signingCertificatePath, string.Empty));
        }

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        //
        // Note: unlike other samples, this sample doesn't use token endpoint pass-through
        // to handle token requests in a custom MVC action. As such, the token requests
        // will be automatically handled by OpenIddict, that will reuse the identity
        // resolved from the authorization code to produce access and identity tokens.
        //
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough();
    })
    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

// 認証を有効にする場合の設定
if (enableAuthorization)
{
    // OpenIddictのクライアント設定を有効にする
    openIddictBuilder
        .AddClient(options =>
        {
            // Enable the client credentials flow. 
            options.AllowAuthorizationCodeFlow();

            // Register the signing and encryption credentials used to protect
            // sensitive data like the state tokens produced by OpenIddict.
            if (builder.Environment.IsDevelopment())
            {
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();
            }
            else
            {
                // Generate a certificate at startup and register it.
                const string encryptionCertificatePath = "cert/client-encryption-certificate.pfx";
                const string signingCertificatePath = "cert/client-signing-certificate.pfx";
                EnsureCertificateExists(
                    encryptionCertificatePath,
                    "CN=Fabrikam Client Encryption Certificate",
                    X509KeyUsageFlags.KeyEncipherment);
                EnsureCertificateExists(
                    signingCertificatePath,
                    "CN=Fabrikam Client Signing Certificate",
                    X509KeyUsageFlags.DigitalSignature);

                options.AddEncryptionCertificate(
                    new X509Certificate2(encryptionCertificatePath, string.Empty));
                options.AddSigningCertificate(
                    new X509Certificate2(signingCertificatePath, string.Empty));
            }

            // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
            options.UseAspNetCore()
                .EnableRedirectionEndpointPassthrough();

            // Register the System.Net.Http integration and use the identity of the current
            // assembly as a more specific user agent, which can be useful when dealing with
            // providers that use the user agent as a way to throttle requests (e.g Reddit).
            options.UseSystemNetHttp()
                .SetProductInformation(typeof(Program).Assembly);

            // Register the Web providers integrations.
            options.UseWebProviders()
                .AddDiscord(discord =>
                {
                    discord.AddScopes("identify", "guilds", "guilds.members.read");
                    discord
                        .SetClientId(builder.Configuration["Discord:ClientId"] ?? "")
                        .SetClientSecret(builder.Configuration["Discord:ClientSecret"] ?? "")
                        .SetRedirectUri("auth/callback");
                });
        });
}

builder.Services.AddAuthorization();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("TrainPolicy", policy =>
    {
        policy.Requirements.Add(new DiscordRoleRequirement
        {
            Condition = role => role.IsDriver || role.IsConductor
        });
    })
    .AddPolicy("TIDPolicy", policy =>
    {
        policy.Requirements.Add(new DiscordRoleRequirement
        {
            Condition = role => role.IsCommander || role.IsDriverManager
        });
    })
    .AddPolicy("InterlockingPolicy", policy =>
    {
        policy.Requirements.Add(new DiscordRoleRequirement
        {
            Condition = role => role.IsSignalman
        });
    })
    .AddPolicy("CommanderTablePolicy", policy =>
    {
        policy.Requirements.Add(new DiscordRoleRequirement
        {
            Condition = role => role.IsCommander
        });
    });


// DI周り
builder.Services
    .AddScoped<IDateTimeRepository, DateTimeRepository>()
    .AddScoped<IDestinationButtonRepository, DestinationButtonRepository>()
    .AddScoped<IDirectionRouteRepository, DirectionRouteRepository>()
    .AddScoped<IDirectionSelfControlLeverRepository, DirectionSelfControlLeverRepository>()
    .AddScoped<IGeneralRepository, GeneralRepository>()
    .AddScoped<IInterlockingObjectRepository, InterlockingObjectRepository>()
    .AddScoped<ILockRepository, LockRepository>()
    .AddScoped<ILockConditionRepository, LockConditionRepository>()
    .AddScoped<ILeverRepository, LeverRepository>()
    .AddScoped<INextSignalRepository, NextSignalRepository>()
    .AddScoped<IOperationNotificationRepository, OperationNotificationRepository>()
    .AddScoped<IProtectionRepository, ProtectionRepository>()
    .AddScoped<IRouteRepository, RouteRepository>()
    .AddScoped<IRouteLeverDestinationRepository, RouteLeverDestinationRepository>()
    .AddScoped<IRouteLockTrackCircuitRepository, RouteLockTrackCircuitRepository>()
    .AddScoped<ISignalRepository, SignalRepository>()
    .AddScoped<ISignalRouteRepository, SignalRouteRepository>()
    .AddScoped<IStationRepository, StationRepository>()
    .AddScoped<ISwitchingMachineRepository, SwitchingMachineRepository>()
    .AddScoped<ISwitchingMachineRouteRepository, SwitchingMachineRouteRepository>()
    .AddScoped<IThrowOutControlRepository, ThrowOutControlRepository>()
    .AddScoped<ITrackCircuitRepository, TrackCircuitRepository>()
    .AddScoped<InterlockingService>()
    .AddScoped<OperationNotificationService>()
    .AddScoped<ProtectionService>()
    .AddScoped<RendoService>()
    .AddScoped<SignalService>()
    .AddScoped<StationService>()
    .AddScoped<SwitchingMachineService>()
    .AddScoped<TrackCircuitService>()
    .AddSingleton(provider =>
    {
        var discordService = new DiscordService(
            builder.Configuration,
            provider.GetRequiredService<IDiscordRepository>(),
            enableAuthorization);
        return discordService;
    })
    .AddSingleton<DiscordRepository>()
    .AddSingleton<IDiscordRepository>(provider => provider.GetRequiredService<DiscordRepository>())
    .AddSingleton<IAuthorizationHandler, DiscordRoleHandler>();
// HostedServiceまわり
builder.Services
    .AddHostedService<InitDbHostedService>();
if (enableAuthorization)
{
    // 認可を使う場合はDiscord BOTの起動をする
    builder.Services.AddHostedService<DiscordBotHostedService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseForwardedHeaders();
    app.UseHsts();
}
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Create a new application registration matching the values configured in MultiATS_Client.
// Note: in a real world application, this step should be part of a setup script.
if (enableAuthorization)
{
    await using var scope = app.Services.CreateAsyncScope();
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    if (await manager.FindByClientIdAsync("MultiATS_Client") == null)
    {
        await manager.CreateAsync(new()
        {
            ApplicationType = ApplicationTypes.Native,
            ClientId = "MultiATS_Client",
            ClientType = ClientTypes.Public,
            RedirectUris =
            {
                new("http://localhost:49152/"),
                new("http://localhost:49153/"),
                new("http://localhost:49154/"),
                new("http://localhost:49155/"),
                new("http://localhost:49156/")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code
            }
        });
    }
}

List<IEndpointConventionBuilder> conventionBuilders =
[
    app.MapControllers(),
    app.MapHub<TrainHub>("/hub/train"),
    app.MapHub<TIDHub>("/hub/TID"),
    app.MapHub<InterlockingHub>("/hub/interlocking"),
    app.MapHub<CommanderTableHub>("/hub/commander_table"),
];
if (!enableAuthorization)
{
    conventionBuilders.ForEach(conventionBuilder => conventionBuilder.AllowAnonymous());
}

app.Run();
return;

static void EnsureCertificateExists(string certificatePath, string subjectName, X509KeyUsageFlags keyUsageFlags)
{
    if (File.Exists(certificatePath))
    {
        return;
    }

    using var algorithm = RSA.Create(2048);

    var subject = new X500DistinguishedName(subjectName);
    var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsageFlags, true));

    var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));

    File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx, string.Empty));
}

public partial class Program;