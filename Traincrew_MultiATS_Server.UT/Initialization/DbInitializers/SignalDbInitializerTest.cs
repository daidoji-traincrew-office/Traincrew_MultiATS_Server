using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class SignalDbInitializerTest
{
    private readonly Mock<ILogger<SignalDbInitializer>> _loggerMock = new();
    private readonly Mock<ISignalRepository> _signalRepositoryMock = new();
    private readonly Mock<INextSignalRepository> _nextSignalRepositoryMock = new();
    private readonly Mock<ISignalRouteRepository> _signalRouteRepositoryMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<IStationRepository> _stationRepositoryMock = new();
    private readonly Mock<IDirectionRouteRepository> _directionRouteRepositoryMock = new();
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    [Fact]
    [DisplayName("データが有効な場合、信号機が正常に追加されること")]
    public async Task InitializeSignalsAsync_ShouldAddSignals_WhenDataIsValid()
    {
        // Arrange
        var signalCsvList = new List<SignalCsv>
        {
            new()
            {
                Name = "Signal1",
                TypeName = "Type1",
                TrackCircuitName = "TC1",
                DirectionRouteLeft = null,
                DirectionRouteRight = null,
                Direction = null,
                RouteNames = null,
                NextSignalNames = null
            }
        };

        var trackCircuits = new Dictionary<string, ulong> { { "TC1", 100 } };
        var stations = new List<Station>
        {
            new() { Id = "ST1", Name = "Signal", IsStation = true, IsPassengerStation = false }
        };

        _signalRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuits);
        _stationRepositoryMock.Setup(r => r.GetWhereIsStation()).ReturnsAsync(stations);
        _directionRouteRepositoryMock.Setup(r => r.GetIdsByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalsAsync(signalCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<IEnumerable<Signal>>(signals =>
                signals.Count() == 1 &&
                signals.ElementAt(0).Name == "Signal1" &&
                signals.ElementAt(0).TypeName == "Type1" &&
                signals.ElementAt(0).TrackCircuitId == 100 &&
                signals.ElementAt(0).StationId == "ST1"
            ), TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の信号機が存在する場合、スキップされること")]
    public async Task InitializeSignalsAsync_ShouldSkipExistingSignals()
    {
        // Arrange
        var signalCsvList = new List<SignalCsv>
        {
            new()
            {
                Name = "Signal1",
                TypeName = "Type1",
                TrackCircuitName = null,
                DirectionRouteLeft = null,
                DirectionRouteRight = null,
                Direction = null,
                RouteNames = null,
                NextSignalNames = null
            }
        };

        _signalRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Signal1"]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _stationRepositoryMock.Setup(r => r.GetWhereIsStation()).ReturnsAsync([]);
        _directionRouteRepositoryMock.Setup(r => r.GetIdsByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalsAsync(signalCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<IEnumerable<Signal>>(list => list.Count() == 0), TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    [DisplayName("方向進路が見つからない場合、例外がスローされること")]
    public async Task InitializeSignalsAsync_ShouldThrowException_WhenDirectionRouteNotFound()
    {
        // Arrange
        var signalCsvList = new List<SignalCsv>
        {
            new()
            {
                Name = "Signal1",
                TypeName = "Type1",
                TrackCircuitName = null,
                DirectionRouteLeft = "NonExistent",
                DirectionRouteRight = null,
                Direction = null,
                RouteNames = null,
                NextSignalNames = null
            }
        };

        _signalRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _stationRepositoryMock.Setup(r => r.GetWhereIsStation()).ReturnsAsync([]);
        _directionRouteRepositoryMock.Setup(r => r.GetIdsByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await initializer.InitializeSignalsAsync(signalCsvList, TestContext.Current.CancellationToken));
    }

    [Fact]
    [DisplayName("データが有効な場合、次位信号機が正常に追加されること")]
    public async Task InitializeNextSignalsAsync_ShouldAddNextSignals_WhenDataIsValid()
    {
        // Arrange
        var signalCsvList = new List<SignalCsv>
        {
            new()
            {
                Name = "Signal1",
                TypeName = "Type1",
                TrackCircuitName = null,
                DirectionRouteLeft = null,
                DirectionRouteRight = null,
                Direction = null,
                RouteNames = null,
                NextSignalNames = ["Signal2"]
            }
        };

        _nextSignalRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _nextSignalRepositoryMock.Setup(r => r.GetAllByDepth(1))
            .ReturnsAsync([]);
        _signalRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync([]);

        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeNextSignalsAsync(signalCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<IEnumerable<NextSignal>>(list =>
                list.Count() == 1 &&
                list.First().SignalName == "Signal1" &&
                list.First().SourceSignalName == "Signal1" &&
                list.First().TargetSignalName == "Signal2" &&
                list.First().Depth == 1
            ), TestContext.Current.CancellationToken),
            Times.AtLeastOnce);
    }

    [Fact]
    [DisplayName("データが有効な場合、信号機進路が正常に追加されること")]
    public async Task InitializeSignalRoutesAsync_ShouldAddSignalRoutes_WhenDataIsValid()
    {
        // Arrange
        var signalCsvList = new List<SignalCsv>
        {
            new()
            {
                Name = "Signal1",
                TypeName = "Type1",
                TrackCircuitName = null,
                DirectionRouteLeft = null,
                DirectionRouteRight = null,
                Direction = null,
                RouteNames = ["Route1"],
                NextSignalNames = null
            }
        };

        var signalRoutes = new List<SignalRoute>();
        var routeIdByName = new Dictionary<string, ulong> { { "Route1", 1 } };

        _signalRouteRepositoryMock.Setup(r => r.GetAllWithRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(signalRoutes);
        _routeRepositoryMock.Setup(r => r.GetIdsByName(It.IsAny<CancellationToken>())).ReturnsAsync(routeIdByName);

        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalRoutesAsync(signalCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<IEnumerable<SignalRoute>>(list =>
                list.Count() == 1 &&
                list.First().SignalName == "Signal1" &&
                list.First().RouteId == 1
            ), TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の信号機進路が存在する場合、スキップされること")]
    public async Task InitializeSignalRoutesAsync_ShouldSkipExistingRoutes()
    {
        // Arrange
        var signalCsvList = new List<SignalCsv>
        {
            new()
            {
                Name = "Signal1",
                TypeName = "Type1",
                TrackCircuitName = null,
                DirectionRouteLeft = null,
                DirectionRouteRight = null,
                Direction = null,
                RouteNames = ["Route1"],
                NextSignalNames = null
            }
        };

        var signalRoutes = new List<SignalRoute>
        {
            new()
            {
                SignalName = "Signal1",
                RouteId = 1,
                Route = new() { Id = 1, Name = "Route1" }
            }
        };
        var routeIdByName = new Dictionary<string, ulong> { { "Route1", 1 } };

        _signalRouteRepositoryMock.Setup(r => r.GetAllWithRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(signalRoutes);
        _routeRepositoryMock.Setup(r => r.GetIdsByName(It.IsAny<CancellationToken>())).ReturnsAsync(routeIdByName);

        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalRoutesAsync(signalCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<IEnumerable<SignalRoute>>(list => list.Count() == 0),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    [DisplayName("初期化処理が何もせずに完了すること")]
    public async Task InitializeAsync_ShouldCompleteWithoutAction()
    {
        // Arrange
        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<IEnumerable<Signal>>(), TestContext.Current.CancellationToken),
            Times.Never);
    }

    [Theory]
    [InlineData("上り閉塞6", "上り6T")]
    [InlineData("下り閉塞8", "下り8T")]
    public async Task InitializeSignalsAsync_ShouldHandleBlockSignals(string signalName, string expectedTcName)
    {
        // Arrange
        var signalCsvList = new List<SignalCsv>
        {
            new()
            {
                Name = signalName,
                TypeName = "Type1",
                TrackCircuitName = null,
                DirectionRouteLeft = null,
                DirectionRouteRight = null,
                Direction = null,
                RouteNames = null,
                NextSignalNames = null
            }
        };

        var trackCircuits = new Dictionary<string, ulong> { { expectedTcName, 100 } };

        _signalRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuits);
        _stationRepositoryMock.Setup(r => r.GetWhereIsStation()).ReturnsAsync([]);
        _directionRouteRepositoryMock.Setup(r => r.GetIdsByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = new SignalDbInitializer(
            _loggerMock.Object,
            _signalRepositoryMock.Object,
            _nextSignalRepositoryMock.Object,
            _signalRouteRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _stationRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalsAsync(signalCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<IEnumerable<Signal>>(list =>
                list.Count() == 1 &&
                list.First().TrackCircuitId == 100
            ), TestContext.Current.CancellationToken),
            Times.Once);
    }
}