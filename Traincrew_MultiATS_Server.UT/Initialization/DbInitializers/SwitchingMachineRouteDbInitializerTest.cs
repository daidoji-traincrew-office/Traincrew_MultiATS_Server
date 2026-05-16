using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class SwitchingMachineRouteDbInitializerTest
{
    private readonly Mock<ILogger<SwitchingMachineRouteDbInitializer>> _loggerMock = new();
    private readonly Mock<ISwitchingMachineRepository> _switchingMachineRepositoryMock = new();
    private readonly Mock<ISwitchingMachineRouteRepository> _switchingMachineRouteRepositoryMock = new();
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<ILockConditionRepository> _lockConditionRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    [Fact]
    [DisplayName("データが有効な場合、転てつ機進路関連が正常に追加されること")]
    public async Task InitializeAsync_ShouldAddSwitchingMachineRoutes_WhenDataIsValid()
    {
        // Arrange
        var routeIds = new List<ulong> { 1, 2 };
        var switchingMachineIds = new HashSet<ulong> { 10, 20 };
        var trackCircuitNames = new List<string> { "TC1", "TC2" };
        var trackCircuitIdByName = new Dictionary<string, ulong> { { "TC1", 100 }, { "TC2", 200 } };
        var existingPairs = new HashSet<(ulong, ulong)>();

        var directLockConditions = new Dictionary<ulong, List<LockCondition>>
        {
            {
                1, [new LockConditionObject { ObjectId = 10, IsReverse = NR.Normal }]
            }
        };

        var routeLockConditions = new Dictionary<ulong, List<LockCondition>>
        {
            {
                1, [new LockConditionObject { ObjectId = 100 }]
            }
        };

        var detectorLockConditions = new Dictionary<ulong, List<LockCondition>>
        {
            {
                10, [new LockConditionObject { ObjectId = 100 }]
            }
        };

        _routeRepositoryMock.Setup(r => r.GetIdsForAll()).ReturnsAsync(routeIds);
        _switchingMachineRepositoryMock.Setup(r => r.GetAllIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(switchingMachineIds);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitNames);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);
        _switchingMachineRouteRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPairs);
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(routeIds, LockType.Lock))
            .ReturnsAsync(directLockConditions);
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(routeIds, LockType.Route))
            .ReturnsAsync(routeLockConditions);
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(switchingMachineIds.ToList(), LockType.Detector))
            .ReturnsAsync(detectorLockConditions);

        var initializer = new SwitchingMachineRouteDbInitializer(
            _loggerMock.Object,
            _switchingMachineRepositoryMock.Object,
            _switchingMachineRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _lockConditionRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SwitchingMachineRoute>>(list =>
                list.Count == 1 &&
                list[0].RouteId == 1 &&
                list[0].SwitchingMachineId == 10 &&
                list[0].IsReverse == NR.Normal &&
                list[0].OnRouteLock == true
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存のペアが存在する場合、スキップされること")]
    public async Task InitializeAsync_ShouldSkipExistingPairs()
    {
        // Arrange
        var routeIds = new List<ulong> { 1 };
        var switchingMachineIds = new HashSet<ulong> { 10 };
        var existingPairs = new HashSet<(ulong, ulong)> { (1, 10) };
        var trackCircuitIdByName = new Dictionary<string, ulong> { { "TC1", 100 } };

        var directLockConditions = new Dictionary<ulong, List<LockCondition>>
        {
            {
                1, [new LockConditionObject { ObjectId = 10, IsReverse = NR.Normal }]
            }
        };

        _routeRepositoryMock.Setup(r => r.GetIdsForAll()).ReturnsAsync(routeIds);
        _switchingMachineRepositoryMock.Setup(r => r.GetAllIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(switchingMachineIds);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);
        _switchingMachineRouteRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPairs);
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(routeIds, LockType.Lock))
            .ReturnsAsync(directLockConditions);
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(routeIds, LockType.Route))
            .ReturnsAsync(new Dictionary<ulong, List<LockCondition>>());
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(switchingMachineIds.ToList(), LockType.Detector))
            .ReturnsAsync(new Dictionary<ulong, List<LockCondition>>());

        var initializer = new SwitchingMachineRouteDbInitializer(
            _loggerMock.Object,
            _switchingMachineRepositoryMock.Object,
            _switchingMachineRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _lockConditionRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SwitchingMachineRoute>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleEmptyData()
    {
        // Arrange
        _routeRepositoryMock.Setup(r => r.GetIdsForAll()).ReturnsAsync([]);
        _switchingMachineRepositoryMock.Setup(r => r.GetAllIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _switchingMachineRouteRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, ulong)>());
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(It.IsAny<List<ulong>>(), It.IsAny<LockType>()))
            .ReturnsAsync(new Dictionary<ulong, List<LockCondition>>());

        var initializer = new SwitchingMachineRouteDbInitializer(
            _loggerMock.Object,
            _switchingMachineRepositoryMock.Object,
            _switchingMachineRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _lockConditionRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SwitchingMachineRoute>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
