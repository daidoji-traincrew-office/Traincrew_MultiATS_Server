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
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.OperationInformation;
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
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainCar;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;
using Traincrew_MultiATS_Server.Repositories.Transaction;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Crew;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var isDevelopment = builder.Environment.IsDevelopment();
        var enableAuthorization = !isDevelopment || builder.Configuration.GetValue<bool>("EnableAuthorization");

        ConfigureServices(builder, isDevelopment, enableAuthorization);

        var app = builder.Build();

        await Configure(app, isDevelopment, enableAuthorization);

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder, bool isDevelopment, bool enableAuthorization)
    {
        ConfigureLoggingService(builder);
        if (!isDevelopment)
        {
            ConfigureProxiedHeadersService(builder);
        }
        ConfigureControllersService(builder);
        if (isDevelopment)
        {
            ConfigureSwaggerService(builder);
        }
        ConfigureDatabaseService(builder);
        ConfigureSignalRService(builder);
        ConfigureAuthenticationService(builder);
        var openiddictBuilder = ConfigureOpeniddictService(builder, isDevelopment);
        if (enableAuthorization)
        {
            ConfigureOpeniddictClientService(builder, openiddictBuilder, isDevelopment);
        }
        ConfigureAuthorizationService(builder);
        ConfigureDependencyInjectionService(builder, enableAuthorization);
        ConfigureHostedServices(builder);
        if (enableAuthorization)
        {
            ConfigureAuthorizationHostedServices(builder);
        }
    }

    private static async Task Configure(WebApplication app, bool isDevelopment, bool enableAuthorization)
    {
        ConfigureHttpLogging(app);
        if (isDevelopment)
        {
            ConfigureSwagger(app);
        }
        else
        {
            ConfigureForwardedHeaders(app);
        }
        ConfigureRouting(app);
        ConfigureCors(app);
        ConfigureAuthentication(app);
        ConfigureAuthorization(app);

        var endpointBuilders = ConfigureEndpoints(app);
        if (!enableAuthorization)
        {
            endpointBuilders.ForEach(builder => builder.AllowAnonymous());
        }
        else
        {
            // OpenIddictアプリケーション登録
            await RegisterOpeniddictApplicationAsync(app);
        }
    }

    private static void ConfigureLoggingService(WebApplicationBuilder builder)
    {
        // ログの設定
        builder.Services.AddHttpLogging(options => { options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders; });
    }

    private static void ConfigureHttpLogging(WebApplication app)
    {
        app.UseHttpLogging();
    }

    private static void ConfigureProxiedHeadersService(WebApplicationBuilder builder)
    {
        // Proxy Headerの設定
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

    private static void ConfigureForwardedHeaders(WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseHsts();
    }

    private static void ConfigureControllersService(WebApplicationBuilder builder)
    {
        // ControllerをServiceに追加
        builder.Services.AddControllers();
    }

    private static void ConfigureRouting(WebApplication app)
    {
        app.UseRouting();
    }

    private static void ConfigureCors(WebApplication app)
    {
        app.UseCors();
    }

    private static void ConfigureSwaggerService(WebApplicationBuilder builder)
    {
        // Swagger/OpenAPIの設定方法は https://aka.ms/aspnetcore/swashbuckle を参照
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    private static void ConfigureSwagger(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    private static void ConfigureDatabaseService(WebApplicationBuilder builder)
    {
        // DBの設定
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
        // Enumのマッピング
        EnumTypeMapper.MapEnumForNpgsql(dataSourceBuilder);
        var dataSource = dataSourceBuilder.Build();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(dataSource);
            // Todo: セッションであることを考えると、Redisを使ったほうが良いかも
            options.UseOpenIddict();
        });
    }

    private static void ConfigureSignalRService(WebApplicationBuilder builder)
    {
        // SignalRの設定
        builder.Services.AddSignalR();
    }

    private static List<IEndpointConventionBuilder> ConfigureEndpoints(WebApplication app)
    {
        // エンドポイントの設定
        return
        [
            app.MapControllers(),
            app.MapHub<TrainHub>("/hub/train"),
            app.MapHub<TIDHub>("/hub/TID"),
            app.MapHub<InterlockingHub>("/hub/interlocking"),
            app.MapHub<CommanderTableHub>("/hub/commander_table"),
        ];
    }

    private static void ConfigureAuthenticationService(WebApplicationBuilder builder)
    {
        // 認可周り
        // Cookie認可の設定
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => { options.ExpireTimeSpan = TimeSpan.FromMinutes(10); });
    }
    
    private static OpenIddictBuilder ConfigureOpeniddictService(WebApplicationBuilder builder, bool isDevelopment)
    {
        // OpenIddictの設定
        return builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                // Authorizationとtokenエンドポイントを有効にする
                options.SetAuthorizationEndpointUris("auth/authorize")
                    .SetTokenEndpointUris("token");

                // AuthorizationCodeFlowとRefreshTokenFlowを有効にする
                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow();
               
                // トークンの有効期限を設定する
                options
                    .SetAccessTokenLifetime(TimeSpan.FromHours(6))
                    .SetIdentityTokenLifetime(TimeSpan.FromHours(6))
                    .SetRefreshTokenLifetime(TimeSpan.FromDays(7));

                // 証明書制御
                if (isDevelopment)
                {
                    options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    // 起動時に証明書を生成して登録する
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

                // ASP.NET Coreホストを登録して、ASP.NET Core固有のオプションを設定する
                //
                // このサーバーではtokenエンドポイントのパススルーを使わない
                // カスタムMVCアクションでtokenリクエストを処理しないので、
                // tokenリクエストはOpenIddictが自動的に処理して、authorization codeから
                // アクセストークンとIDトークンを発行する
                //
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                // ローカルのOpenIddictサーバーインスタンスから設定をインポートする
                options.UseLocalServer();
                // ASP.NET Coreホストを登録する
                options.UseAspNetCore();
            });
    }

    private static void ConfigureOpeniddictClientService(
        WebApplicationBuilder builder, OpenIddictBuilder openiddictBuilder, bool isDevelopment)
    {
        // クライアント設定
        openiddictBuilder
            .AddClient(options =>
            {
                // AuthorizationCodeFlowを有効にする
                options.AllowAuthorizationCodeFlow();

                // 証明書制御
                if (isDevelopment)
                {
                    options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    // 起動時に証明書を生成して登録する
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

                // ASP.NET Coreホストを登録してリダイレクトエンドポイントのパススルーを有効にする
                options.UseAspNetCore()
                    .EnableRedirectionEndpointPassthrough();

                // System.Net.Http統合とUserAgent
                options.UseSystemNetHttp()
                    .SetProductInformation(typeof(Program).Assembly);

                // Discord Web Provider
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

    private static void ConfigureAuthentication(WebApplication app)
    {
        app.UseAuthentication();
    }

    private static void ConfigureAuthorizationService(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureAuthorization(WebApplication app)
    {
        app.UseAuthorization();
    }

    private static async Task RegisterOpeniddictApplicationAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("MultiATS_Client") != null)
        {
            // 既に登録されている場合は何もしない
            return;
        }
        var applicationDescriptor = new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Native,
            ClientId = "MultiATS_Client",
            ClientType = ClientTypes.Public,
            Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.GrantTypes.RefreshToken
                }
        };

        // ローカルで複数ポートで動かす場合のリダイレクトURIを全部登録する
        Enumerable.Range(0, 10)
            .Select(i => new Uri($"http://localhost:{49152 + i}/"))
            .ToList()
            .ForEach(uri => applicationDescriptor.RedirectUris.Add(uri));

        await manager.CreateAsync(applicationDescriptor);
    }

    private static void ConfigureDependencyInjectionService(WebApplicationBuilder builder, bool enableAuthorization)
    {
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
            .AddScoped<IOperationInformationRepository, OperationInformationRepository>()
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
            .AddScoped<ITrainRepository, TrainRepository>()
            .AddScoped<ITrainCarRepository, TrainCarRepository>()
            .AddScoped<ITrainDiagramRepository, TrainDiagramRepository>()
            .AddScoped<ITtcWindowRepository, TtcWindowRepository>()
            .AddScoped<ITtcWindowLinkRepository, TtcWindowLinkRepository>()
            .AddScoped<ITransactionRepository, TransactionRepository>()
            .AddScoped<DirectionRouteService>()
            .AddScoped<InterlockingService>()
            .AddScoped<OperationNotificationService>()
            .AddScoped<OperationInformationService>()
            .AddScoped<ProtectionService>()
            .AddScoped<RendoService>()
            .AddScoped<RouteService>()
            .AddScoped<SignalService>()
            .AddScoped<StationService>()
            .AddScoped<SwitchingMachineService>()
            .AddScoped<TrackCircuitService>()
            .AddScoped<TrainService>()
            .AddScoped<TIDService>()
            .AddScoped<TtcStationControlService>()
            .AddSingleton<EnableAuthorizationStore>(_ => new(enableAuthorization))
            .AddSingleton<DiscordService>()
            .AddSingleton<DiscordRepository>()
            .AddSingleton<IDiscordRepository>(provider => provider.GetRequiredService<DiscordRepository>())
            .AddSingleton<IMutexRepository, MutexRepository>()
            .AddSingleton<IAuthorizationHandler, DiscordRoleHandler>();
    }

    private static void ConfigureHostedServices(WebApplicationBuilder builder)
    {
        // HostedServiceまわり
        builder.Services.AddHostedService<InitDbHostedService>();
    }

    private static void ConfigureAuthorizationHostedServices(WebApplicationBuilder builder)
    {
        // 認可を使う場合はDiscord BOTの起動をする
        builder.Services.AddHostedService<DiscordBotHostedService>();
    }

    private static void EnsureCertificateExists(string certificatePath, string subjectName, X509KeyUsageFlags keyUsageFlags)
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
}