using Microsoft.EntityFrameworkCore;
using Npgsql;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;
using Traincrew_MultiATS_Server.Repositories.Protection;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainCar;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Passenger;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var isDevelopment = builder.Environment.IsDevelopment();

        ConfigureServices(builder, isDevelopment);

        var app = builder.Build();

        await Configure(app, isDevelopment);

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder, bool isDevelopment)
    {
        // Controller
        builder.Services.AddControllers();
        
        // Database
        ConfigureDatabaseService(builder);

        // Swagger
        if (isDevelopment)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }

        // Todo: CORS設定, Rate Limit設定, CSP設定
        // Todo: (優先度低)キャッシュ制御, Response Compression

        // DI
        builder.Services
            .AddScoped<IGeneralRepository, GeneralRepository>()
            .AddScoped<ITrackCircuitRepository, TrackCircuitRepository>()
            .AddScoped<ITrainRepository, TrainRepository>()
            .AddScoped<ITrainCarRepository, TrainCarRepository>()
            .AddScoped<ITrainDiagramRepository, TrainDiagramRepository>()
            .AddScoped<INextSignalRepository, NextSignalRepository>()
            .AddScoped<ISignalRepository, SignalRepository>()
            .AddScoped<IOperationNotificationRepository, OperationNotificationRepository>()
            .AddScoped<IProtectionRepository, ProtectionRepository>()
            .AddScoped<IRouteRepository, RouteRepository>()
            .AddScoped<ISignalRouteRepository, SignalRouteRepository>()
            .AddScoped<IDateTimeRepository, DateTimeRepository>()
            .AddScoped<TrackCircuitService>()
            .AddScoped<SignalService>()
            .AddScoped<OperationNotificationService>()
            .AddScoped<ProtectionService>()
            .AddScoped<RouteService>()
            .AddScoped<TrainService>()
            .AddScoped<PassengerService>();
    }

    private static async Task Configure(WebApplication app, bool isDevelopment)
    {
        if (isDevelopment)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHsts();
        app.UseHttpsRedirection();

        app.MapControllers();

        await Task.CompletedTask;
    }
    
    private static void ConfigureDatabaseService(WebApplicationBuilder builder)
    {
        // DBの設定
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
        EnumTypeMapper.MapEnumForNpgsql(dataSourceBuilder); 
        var dataSource = dataSourceBuilder.Build();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(dataSource);
        });
    }
}