using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
using Traincrew_MultiATS_Server.Repositories.TrackCircuitSignal;
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainCar;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;
using Traincrew_MultiATS_Server.Repositories.TrainSignalState;
using Traincrew_MultiATS_Server.Repositories.TrainType;
using Traincrew_MultiATS_Server.Repositories.Transaction;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLinkRouteCondition;
using Traincrew_MultiATS_Server.Repositories.TtcWindowTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.UserDisconnection;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.TestHelpers;

/// <summary>
/// Provides extension methods to register all Repository and Service mocks for unit testing.
/// Use AddAllMocks() to automatically register mocks for all dependencies,
/// then customize specific mocks with ReplaceMock() or UseRealService().
/// </summary>
public static class MockDependencyProvider
{
    /// <summary>
    /// Registers mock instances for all 41 Repositories and 21 Services.
    /// This allows unit tests to focus on testing a single service in isolation.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    public static void AddAllMocks(this IServiceCollection services)
    {
        // Register all Repository mocks (41 repositories)
        services.AddScoped(_ => new Mock<IDateTimeRepository>().Object);
        services.AddScoped(_ => new Mock<IDestinationButtonRepository>().Object);
        services.AddScoped(_ => new Mock<IDirectionRouteRepository>().Object);
        services.AddScoped(_ => new Mock<IDirectionSelfControlLeverRepository>().Object);
        services.AddScoped(_ => new Mock<IDiscordRepository>().Object);
        services.AddScoped(_ => new Mock<IGeneralRepository>().Object);
        services.AddScoped(_ => new Mock<IInterlockingObjectRepository>().Object);
        services.AddScoped(_ => new Mock<ILeverRepository>().Object);
        services.AddScoped(_ => new Mock<ILockRepository>().Object);
        services.AddScoped(_ => new Mock<ILockConditionRepository>().Object);
        services.AddScoped(_ => new Mock<ILockConditionByRouteCentralControlLeverRepository>().Object);
        services.AddScoped(_ => new Mock<IMutexRepository>().Object);
        services.AddScoped(_ => new Mock<INextSignalRepository>().Object);
        services.AddScoped(_ => new Mock<IOperationInformationRepository>().Object);
        services.AddScoped(_ => new Mock<IOperationNotificationRepository>().Object);
        services.AddScoped(_ => new Mock<IOperationNotificationDisplayRepository>().Object);
        services.AddScoped(_ => new Mock<IProtectionRepository>().Object);
        services.AddScoped(_ => new Mock<IRouteRepository>().Object);
        services.AddScoped(_ => new Mock<IRouteCentralControlLeverRepository>().Object);
        services.AddScoped(_ => new Mock<IRouteLeverDestinationRepository>().Object);
        services.AddScoped(_ => new Mock<IRouteLockTrackCircuitRepository>().Object);
        services.AddScoped(_ => new Mock<IServerRepository>().Object);
        services.AddScoped(_ => new Mock<ISignalRepository>().Object);
        services.AddScoped(_ => new Mock<ISignalRouteRepository>().Object);
        services.AddScoped(_ => new Mock<ISignalTypeRepository>().Object);
        services.AddScoped(_ => new Mock<IStationRepository>().Object);
        services.AddScoped(_ => new Mock<IStationTimerStateRepository>().Object);
        services.AddScoped(_ => new Mock<ISwitchingMachineRepository>().Object);
        services.AddScoped(_ => new Mock<ISwitchingMachineRouteRepository>().Object);
        services.AddScoped(_ => new Mock<IThrowOutControlRepository>().Object);
        services.AddScoped(_ => new Mock<ITrackCircuitRepository>().Object);
        services.AddScoped(_ => new Mock<ITrackCircuitSignalRepository>().Object);
        services.AddScoped(_ => new Mock<ITrainRepository>().Object);
        services.AddScoped(_ => new Mock<ITrainCarRepository>().Object);
        services.AddScoped(_ => new Mock<ITrainDiagramRepository>().Object);
        services.AddScoped(_ => new Mock<ITrainTypeRepository>().Object);
        services.AddScoped(_ => new Mock<ITrainSignalStateRepository>().Object);
        services.AddScoped(_ => new Mock<ITtcWindowRepository>().Object);
        services.AddScoped(_ => new Mock<ITtcWindowLinkRepository>().Object);
        services.AddScoped(_ => new Mock<ITtcWindowTrackCircuitRepository>().Object);
        services.AddScoped(_ => new Mock<ITtcWindowLinkRouteConditionRepository>().Object);
        services.AddScoped(_ => new Mock<ITransactionRepository>().Object);
        services.AddScoped(_ => new Mock<IUserDisconnectionRepository>().Object);

        // Register all Service mocks (21 services)
        services.AddScoped(_ => new Mock<IBannedUserService>().Object);
        services.AddScoped(_ => new Mock<ICommanderTableService>().Object);
        services.AddScoped(_ => new Mock<ICTCPService>().Object);
        services.AddScoped(_ => new Mock<IDateTimeService>().Object);
        services.AddScoped(_ => new Mock<IDirectionRouteService>().Object);
        services.AddScoped(_ => new Mock<IDiscordService>().Object);
        services.AddScoped(_ => new Mock<IInterlockingService>().Object);
        services.AddScoped(_ => new Mock<IOperationInformationService>().Object);
        services.AddScoped(_ => new Mock<IOperationNotificationService>().Object);
        services.AddScoped(_ => new Mock<IPassengerService>().Object);
        services.AddScoped(_ => new Mock<IProtectionService>().Object);
        services.AddScoped(_ => new Mock<IRendoService>().Object);
        services.AddScoped(_ => new Mock<IRouteService>().Object);
        services.AddScoped(_ => new Mock<IServerService>().Object);
        services.AddScoped(_ => new Mock<ISignalService>().Object);
        services.AddScoped(_ => new Mock<IStationService>().Object);
        services.AddScoped(_ => new Mock<ISwitchingMachineService>().Object);
        services.AddScoped(_ => new Mock<ITIDService>().Object);
        services.AddScoped(_ => new Mock<ITrackCircuitService>().Object);
        services.AddScoped(_ => new Mock<ITrainService>().Object);
        services.AddScoped(_ => new Mock<ITtcStationControlService>().Object);

        // Register common test dependencies
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    /// <summary>
    /// Replaces a previously registered mock with a new mock instance.
    /// Use this when you need to setup specific behavior on a mock for a test.
    /// </summary>
    /// <typeparam name="TInterface">Interface type to replace</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="mock">Mock instance with configured behavior</param>
    public static void ReplaceMock<TInterface>(
        this IServiceCollection services,
        Mock<TInterface> mock) where TInterface : class
    {
        services.RemoveAll<TInterface>();
        services.AddScoped(_ => mock.Object);
    }

    /// <summary>
    /// Replaces a mock registration with the real service implementation.
    /// Use this for the service you're testing (all dependencies remain mocked).
    /// </summary>
    /// <typeparam name="TInterface">Service interface type</typeparam>
    /// <typeparam name="TImplementation">Service implementation type</typeparam>
    /// <param name="services">Service collection</param>
    public static void UseRealService<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.RemoveAll<TInterface>();
        services.AddScoped<TInterface, TImplementation>();
    }

    /// <summary>
    /// Removes all registrations for the specified service type.
    /// </summary>
    private static void RemoveAll<T>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }
}
