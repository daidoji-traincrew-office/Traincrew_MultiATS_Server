using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenIddict.Validation.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Traincrew_MultiATS_Server.Activity;
using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Initialization;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Initialization.JsonLoaders;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.ClosedCircuitLockTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.Diagram;
using Traincrew_MultiATS_Server.Repositories.DiagramTrain;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.Discord;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Lock;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.LockConditionByRouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.OperationInformation;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;
using Traincrew_MultiATS_Server.Repositories.OperationNotificationDisplay;
using Traincrew_MultiATS_Server.Repositories.Protection;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.SignalType;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitSignal;
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainCar;
using Traincrew_MultiATS_Server.Repositories.TrainSignalState;
using Traincrew_MultiATS_Server.Repositories.TrainType;
using Traincrew_MultiATS_Server.Repositories.Transaction;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLinkRouteCondition;
using Traincrew_MultiATS_Server.Repositories.TtcWindowTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.UserDisconnection;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services;
using Traincrew_MultiATS_Server.Services.Cache;

namespace Traincrew_MultiATS_Server.Crew;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var isDevelopment = builder.Environment.IsDevelopment();
        var enableAuthorization = !isDevelopment || builder.Configuration.GetValue<bool>("EnableAuthorization");
        var enableOtlp = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] != null;

        ConfigureServices(builder, isDevelopment, enableAuthorization, enableOtlp);

        var app = builder.Build();

        Configure(app, isDevelopment, enableAuthorization);

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder,
        bool isDevelopment,
        bool enableAuthorization,
        bool enableOtlp)
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

        if (enableOtlp)
        {
            ConfigureOpenTelemetryService(builder);
        }

        ConfigureAuthorizationService(builder, enableAuthorization);
        ConfigureDependencyInjectionService(builder, enableAuthorization);
        ConfigureHostedServices(builder);
        if (enableAuthorization)
        {
            ConfigureAuthorizationHostedServices(builder);
        }
    }

    private static void Configure(
        WebApplication app,
        bool isDevelopment,
        bool enableAuthorization)
    {
        if (isDevelopment)
        {
            ConfigureSwagger(app);
        }
        else
        {
            ConfigureForwardedHeaders(app);
        }

        ConfigureRouting(app);
        // HttpLoggingMiddlewareはエンドポイント単位の設定(/healthzの.WithHttpLogging)を
        // context.GetEndpoint()から読むため、UseRouting()より後に置く必要がある
        ConfigureHttpLogging(app);
        ConfigureCors(app);
        ConfigureAuthentication(app);
        ConfigureAuthorization(app);

        var endpointBuilders = ConfigureEndpoints(app);
        if (!enableAuthorization)
        {
            endpointBuilders.ForEach(builder => builder.AllowAnonymous());
        }
    }

    private static void ConfigureLoggingService(WebApplicationBuilder builder)
    {
        // ログの設定
        builder.Services.AddHttpLogging(options =>
        {
            // 組込みのリクエストログ(Hosting.Diagnostics等)を落とす代わりに、
            // HttpLoggingを1リクエスト=1レコードの唯一のリクエストログとして使う
            options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders
                                    | HttpLoggingFields.ResponseStatusCode
                                    | HttpLoggingFields.Duration;
        });
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
        });
    }

    private static void ConfigureSignalRService(WebApplicationBuilder builder)
    {
        // SignalRの設定
        builder.Services.AddSignalR(options =>
        {
            options.ClientTimeoutInterval = TimeSpan.FromMinutes(3);
        });
    }

    private static List<IEndpointConventionBuilder> ConfigureEndpoints(WebApplication app)
    {
        // エンドポイントの設定
        // 初期化完了(InitDbHostedServiceがDB初期化オーケストレータとスケジューラ起動を完走した後)
        // にのみ200を返すヘルスチェック。認可を通さないため常に明示的にAllowAnonymousとする
        // (enableAuthorization=true時はendpointBuildersのAllowAnonymous一括付与に乗らないため)
        app.MapGet("/healthz", (InitializationState initializationState) =>
                initializationState.IsInitialized ? Results.Ok() : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
            .AllowAnonymous()
            // healthcheckが数秒おきに叩くためログには出さない
            .WithHttpLogging(HttpLoggingFields.None);

        return
        [
            app.MapControllers(),
            app.MapHub<TrainHub>("/hub/train"),
            app.MapHub<TIDHub>("/hub/TID"),
            app.MapHub<CTCPHub>("/hub/CTCP"),
            app.MapHub<InterlockingHub>("/hub/interlocking"),
            app.MapHub<CommanderTableHub>("/hub/commander_table"),
        ];
    }

    private static void ConfigureAuthenticationService(WebApplicationBuilder builder)
    {
        // 認証は出勤台 IdP が発行したアクセストークンの検証だけを行う(Crew 自身はトークンを発行しない)。
        // トークンの sub は Discord ユーザー ID(10進数の文字列)で、Hub はすべて ulong に変換して使う
        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.Services.AddIdpTokenValidation();
    }

    private static void ConfigureAuthentication(WebApplication app)
    {
        app.UseAuthentication();
    }

    private static void ConfigureAuthorizationService(WebApplicationBuilder builder, bool enableAuthorization)
    {
        // 入鋏ロールは、認可が有効なすべてのエンドポイントで必須にする。
        // 名前付きポリシーを指定したエンドポイントには Default/Fallback が効かないので、名前付きポリシーにも足す
        var beginnerRequirement = new DiscordRoleRequirement
        {
            Condition = role => role.IsBeginner
        };
        var beginnerPolicy = new AuthorizationPolicyBuilder(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(beginnerRequirement)
            .Build();

        var authorizationBuilder = builder.Services.AddAuthorizationBuilder()
            // [Authorize] だけを付けたエンドポイント
            .SetDefaultPolicy(beginnerPolicy)
            .AddPolicy("TrainPolicy", policy => AddRoleRequirements(policy, role => role.IsDriver || role.IsConductor))
            .AddPolicy("TIDPolicy", policy => AddRoleRequirements(policy, role => role.IsCommander || role.IsDriverManager))
            .AddPolicy("InterlockingPolicy", policy => AddRoleRequirements(policy, role => role.IsSignalman))
            .AddPolicy("CommanderTablePolicy", policy => AddRoleRequirements(policy, role => role.IsCommander));
        if (enableAuthorization)
        {
            // [Authorize] も [AllowAnonymous] も付いていないエンドポイント(/me など)。
            // どのエンドポイントにも一致しないリクエストにも効くので、存在しないパスは 404 ではなく 401 になる。
            // 認可が無効(開発時)は設定せず、存在しないパスを 404 のままにする
            authorizationBuilder.SetFallbackPolicy(beginnerPolicy);
        }

        return;

        void AddRoleRequirements(AuthorizationPolicyBuilder policy, Predicate<TraincrewRole> condition)
        {
            policy.Requirements.Add(beginnerRequirement);
            policy.Requirements.Add(new DiscordRoleRequirement
            {
                Condition = condition
            });
        }
    }

    private static void ConfigureAuthorization(WebApplication app)
    {
        app.UseAuthorization();
    }

    private static void ConfigureDependencyInjectionService(WebApplicationBuilder builder, bool enableAuthorization)
    {
        // DI周り
        builder.Services.AddMemoryCache();
        builder.Services
            // CSV Loaders
            .AddScoped<StationCsvLoader>()
            .AddScoped<TrackCircuitCsvLoader>()
            .AddScoped<SignalTypeCsvLoader>()
            .AddScoped<TrainTypeCsvLoader>()
            .AddScoped<DiagramTrainCsvLoader>()
            .AddScoped<RendoTableCsvLoader>()
            .AddScoped<OperationNotificationDisplayCsvLoader>()
            .AddScoped<RouteLockTrackCircuitCsvLoader>()
            .AddScoped<TtcWindowCsvLoader>()
            .AddScoped<TtcWindowLinkCsvLoader>()
            .AddScoped<ThrowOutControlCsvLoader>()
            .AddScoped<SignalCsvLoader>()
            // JSON Loaders
            .AddScoped<DiagramJsonLoader>()
            // DB Initializers
            .AddScoped<StationDbInitializer>()
            .AddScoped<TrackCircuitDbInitializer>()
            .AddScoped<SignalTypeDbInitializer>()
            .AddScoped<TrainDbInitializer>()
            .AddScoped<OperationNotificationDisplayDbInitializer>()
            .AddScoped<RouteLockTrackCircuitDbInitializer>()
            .AddScoped<ClosedCircuitLockTrackCircuitDbInitializer>()
            .AddScoped<ServerStatusDbInitializer>()
            .AddScoped<TtcDbInitializer>()
            .AddScoped<ThrowOutControlDbInitializer>()
            .AddScoped<SignalDbInitializer>()
            .AddScoped<InterlockingObjectDbInitializer>()
            .AddScoped<SwitchingMachineRouteDbInitializer>()
            .AddScoped<DiagramDbInitializer>()
            // Orchestrator
            .AddScoped<DatabaseInitializationOrchestrator>()
            // 初期化完了状態(/healthz が参照する)
            .AddSingleton<InitializationState>()
            // キャッシュの充填と無効化の土台
            .AddSingleton<ICacheGate, CacheGate>()
            .AddScoped<IDateTimeRepository, DateTimeRepository>()
            .AddScoped<IDestinationButtonRepository, DestinationButtonRepository>()
            .AddScoped<IDirectionRouteRepository, DirectionRouteRepository>()
            .AddScoped<IDirectionSelfControlLeverRepository, DirectionSelfControlLeverRepository>()
            .AddScoped<IGeneralRepository, GeneralRepository>()
            .AddScoped<IInterlockingObjectRepository, InterlockingObjectRepository>()
            .AddScoped<ILockRepository, LockRepository>()
            .AddScoped<ILockConditionRepository, LockConditionRepository>()
            .AddScoped<ILockConditionByRouteCentralControlLeverRepository, LockConditionByRouteCentralControlLeverRepository>()
            .AddScoped<ILeverRepository, LeverRepository>()
            .AddScoped<INextSignalRepository, NextSignalRepository>()
            .AddScoped<IOperationNotificationRepository, OperationNotificationRepository>()
            .AddScoped<IOperationNotificationDisplayRepository, OperationNotificationDisplayRepository>()
            .AddScoped<IOperationInformationRepository, OperationInformationRepository>()
            .AddScoped<IProtectionRepository, ProtectionRepository>()
            .AddScoped<IRouteRepository, RouteRepository>()
            .AddScoped<IRouteCentralControlLeverRepository, RouteCentralControlLeverRepository>()
            .AddScoped<IRouteLeverDestinationRepository, RouteLeverDestinationRepository>()
            .AddScoped<IRouteLockTrackCircuitRepository, RouteLockTrackCircuitRepository>()
            .AddScoped<IClosedCircuitLockTrackCircuitRepository, ClosedCircuitLockTrackCircuitRepository>()
            .AddScoped<IServerRepository, ServerRepository>()
            .AddScoped<ISignalRepository, SignalRepository>()
            .AddScoped<ISignalRouteRepository, SignalRouteRepository>()
            .AddScoped<ISignalTypeRepository, SignalTypeRepository>()
            .AddScoped<IStationRepository, StationRepository>()
            .AddScoped<IStationTimerStateRepository, StationTimerStateRepository>()
            .AddScoped<ISwitchingMachineRepository, SwitchingMachineRepository>()
            .AddScoped<ISwitchingMachineRouteRepository, SwitchingMachineRouteRepository>()
            .AddScoped<IThrowOutControlRepository, ThrowOutControlRepository>()
            .AddScoped<ITrackCircuitRepository, TrackCircuitRepository>()
            .AddScoped<ITrackCircuitDepartmentTimeRepository, TrackCircuitDepartmentTimeRepository>()
            .AddScoped<ITrackCircuitSignalRepository, TrackCircuitSignalRepository>()
            .AddScoped<ITrainRepository, TrainRepository>()
            .AddScoped<ITrainCarRepository, TrainCarRepository>()
            .AddScoped<IDiagramRepository, DiagramRepository>()
            .AddScoped<IDiagramTrainRepository, DiagramTrainRepository>()
            .AddScoped<ITrainTypeRepository, TrainTypeRepository>()
            .AddScoped<ITrainSignalStateRepository, TrainSignalStateRepository>()
            .AddScoped<ITtcWindowRepository, TtcWindowRepository>()
            .AddScoped<ITtcWindowLinkRepository, TtcWindowLinkRepository>()
            .AddScoped<ITtcWindowTrackCircuitRepository, TtcWindowTrackCircuitRepository>()
            .AddScoped<ITtcWindowLinkRouteConditionRepository, TtcWindowLinkRouteConditionRepository>()
            .AddScoped<ITransactionRepository, TransactionRepository>()
            .AddScoped<IUserDisconnectionRepository, UserDisconnectionRepository>()
            .AddScoped<IBannedUserService, BannedUserService>()
            .AddScoped<ICommanderTableService, CommanderTableService>()
            .AddScoped<ICTCPService, CTCPService>()
            .AddScoped<IDateTimeService, DateTimeService>()
            .AddScoped<IDestinationButtonService, DestinationButtonService>()
            .AddScoped<IDirectionRouteService, DirectionRouteService>()
            .AddScoped<IDirectionSelfControlLeverService, DirectionSelfControlLeverService>()
            .AddScoped<IInterlockingService, InterlockingService>()
            .AddScoped<ILeverService, LeverService>()
            .AddScoped<IRouteCentralControlLeverService, RouteCentralControlLeverService>()
            .AddScoped<IOperationNotificationService, OperationNotificationService>()
            .AddScoped<IOperationInformationService, OperationInformationService>()
            .AddScoped<IProtectionService, ProtectionService>()
            .AddScoped<IRouteService, RouteService>()
            .AddScoped<IServerService, ServerService>()
            .AddScoped<IDiagramService, DiagramService>()
            .AddScoped<ISignalService, SignalService>()
            .AddScoped<IStationService, StationService>()
            .AddScoped<ISwitchingMachineService, SwitchingMachineService>()
            .AddScoped<ITrackCircuitService, TrackCircuitService>()
            .AddScoped<ITrainService, TrainService>()
            .AddScoped<ITIDService, TIDService>()
            .AddScoped<ITtcStationControlService, TtcStationControlService>()
            .AddSingleton<EnableAuthorizationStore>(_ => new(enableAuthorization))
            .AddSingleton<IInterlockingObjectMasterStore, InterlockingObjectMasterStore>()
            .AddSingleton<IOperationNotificationMasterStore, OperationNotificationMasterStore>()
            .AddSingleton<IDiscordService, DiscordService>()
            .AddSingleton<MetricsCollector>()
            .AddSingleton<SchedulerManagerForServer>()
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
        // 認可を使う場合はDiscord BOTの起動と、出勤台 IdP の署名公開鍵の読み込みをする(issuer の設定は必須)
        builder.Services.AddHostedService<DiscordBotHostedService>();
        builder.Services.AddIdpSigningKeyLoading();
    }

    private static void ConfigureOpenTelemetryService(WebApplicationBuilder builder)
    {
        // Setup logging to be exported via OpenTelemetry
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var otel = builder.Services.AddOpenTelemetry();

        // Add Metrics for ASP.NET Core and our custom metrics and export via OTLP
        otel.WithMetrics(metrics =>
        {
            // Metrics provider from OpenTelemetry
            metrics.AddAspNetCoreInstrumentation();
            // Metrics provides by ASP.NET Core in .NET 8
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Http.Connections");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
            // Custom application metrics
            metrics.AddMeter(ApplicationMetrics.Meter.Name);
        });

        // Add Tracing for ASP.NET Core and our custom ActivitySource and export via OTLP
        otel.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            });
            tracing.AddSource(ActivitySources.Scheduler.Name);
            tracing.AddSource(ActivitySources.Hubs.Name);
        });

        otel.UseOtlpExporter();
    }
}
