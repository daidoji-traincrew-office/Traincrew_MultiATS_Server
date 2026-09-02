using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class ThrowOutControlDbInitializerTest
{
    private readonly Mock<ILogger<ThrowOutControlDbInitializer>> _loggerMock = new();
    private readonly Mock<ILogger<ThrowOutControlCsvLoader>> _csvLoaderLoggerMock = new();
    private readonly Mock<ThrowOutControlCsvLoader> _csvLoaderMock;
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<IDirectionRouteRepository> _directionRouteRepositoryMock = new();
    private readonly Mock<IDirectionSelfControlLeverRepository> _directionSelfControlLeverRepositoryMock = new();
    private readonly Mock<IThrowOutControlRepository> _throwOutControlRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    public ThrowOutControlDbInitializerTest()
    {
        _csvLoaderMock = new(_csvLoaderLoggerMock.Object);
    }

    [Fact]
    [DisplayName("てこタイプの総括制御が正常に追加されること")]
    public async Task InitializeAsync_ShouldAddThrowOutControls_WithLeverType()
    {
        // Arrange
        var csvRecords = new List<ThrowOutControlCsv>
        {
            new()
            {
                SourceLever = "Route1",
                TargetLever = "Route2",
                Type = ThrowOutControlType.WithLever,
                LeverCondition = null
            }
        };

        var routesByName = new Dictionary<string, Route>
        {
            { "Route1", new() { Id = 1, Name = "Route1" } },
            { "Route2", new() { Id = 2, Name = "Route2" } }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetByNames(It.IsAny<CancellationToken>())).ReturnsAsync(routesByName);
        _directionRouteRepositoryMock.Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionRoute>());
        _directionSelfControlLeverRepositoryMock
            .Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionSelfControlLever>());
        _throwOutControlRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, ulong)>());

        var initializer = new ThrowOutControlDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _directionSelfControlLeverRepositoryMock.Object,
            _throwOutControlRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<ThrowOutControl>>(list =>
                list.Count == 1 &&
                list[0].SourceId == 1 &&
                list[0].TargetId == 2 &&
                list[0].ControlType == ThrowOutControlType.WithLever &&
                list[0].TargetLr == null &&
                list[0].ConditionLeverId == null
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("方向タイプの総括制御が正常に追加されること")]
    public async Task InitializeAsync_ShouldAddThrowOutControls_DirectionType()
    {
        // Arrange
        var csvRecords = new List<ThrowOutControlCsv>
        {
            new()
            {
                SourceLever = "Route1",
                TargetLever = "DirectionL",
                Type = ThrowOutControlType.Direction,
                LeverCondition = "LeverN"
            }
        };

        var routesByName = new Dictionary<string, Route>
        {
            { "Route1", new() { Id = 1, Name = "Route1" } }
        };

        var directionRoutesByName = new Dictionary<string, DirectionRoute>
        {
            { "DirectionF", new() { Id = 3, Name = "DirectionF" } }
        };

        var directionSelfControlLeversByName = new Dictionary<string, DirectionSelfControlLever>
        {
            { "Lever", new() { Id = 10, Name = "Lever" } }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetByNames(It.IsAny<CancellationToken>())).ReturnsAsync(routesByName);
        _directionRouteRepositoryMock.Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directionRoutesByName);
        _directionSelfControlLeverRepositoryMock
            .Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directionSelfControlLeversByName);
        _throwOutControlRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, ulong)>());

        var initializer = new ThrowOutControlDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _directionSelfControlLeverRepositoryMock.Object,
            _throwOutControlRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<ThrowOutControl>>(list =>
                list.Count == 1 &&
                list[0].SourceId == 1 &&
                list[0].TargetId == 3 &&
                list[0].ControlType == ThrowOutControlType.Direction &&
                list[0].TargetLr == LR.Left &&
                list[0].ConditionLeverId == 10 &&
                list[0].ConditionNr == NR.Normal
            ), It.IsAny<CancellationToken>()),
            Times.Once);

        _directionRouteRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [DisplayName("既存のペアが存在する場合、スキップされること")]
    public async Task InitializeAsync_ShouldSkipExistingPairs()
    {
        // Arrange
        var csvRecords = new List<ThrowOutControlCsv>
        {
            new()
            {
                SourceLever = "Route1",
                TargetLever = "Route2",
                Type = ThrowOutControlType.WithLever,
                LeverCondition = null
            }
        };

        var routesByName = new Dictionary<string, Route>
        {
            { "Route1", new() { Id = 1, Name = "Route1" } },
            { "Route2", new() { Id = 2, Name = "Route2" } }
        };

        var existingPairs = new HashSet<(ulong, ulong)> { (1, 2) };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetByNames(It.IsAny<CancellationToken>())).ReturnsAsync(routesByName);
        _directionRouteRepositoryMock.Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionRoute>());
        _directionSelfControlLeverRepositoryMock
            .Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionSelfControlLever>());
        _throwOutControlRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPairs);

        var initializer = new ThrowOutControlDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _directionSelfControlLeverRepositoryMock.Object,
            _throwOutControlRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<ThrowOutControl>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("発進路が見つからない場合、スキップされること")]
    public async Task InitializeAsync_ShouldSkipWhenSourceRouteNotFound()
    {
        // Arrange
        var csvRecords = new List<ThrowOutControlCsv>
        {
            new()
            {
                SourceLever = "NonExistentRoute",
                TargetLever = "Route2",
                Type = ThrowOutControlType.WithLever,
                LeverCondition = null
            }
        };

        var routesByName = new Dictionary<string, Route>
        {
            { "Route2", new() { Id = 2, Name = "Route2" } }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetByNames(It.IsAny<CancellationToken>())).ReturnsAsync(routesByName);
        _directionRouteRepositoryMock.Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionRoute>());
        _directionSelfControlLeverRepositoryMock
            .Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionSelfControlLever>());
        _throwOutControlRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, ulong)>());

        var initializer = new ThrowOutControlDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _directionSelfControlLeverRepositoryMock.Object,
            _throwOutControlRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<ThrowOutControl>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleEmptyCsvData()
    {
        // Arrange
        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _routeRepositoryMock.Setup(r => r.GetByNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Route>());
        _directionRouteRepositoryMock.Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionRoute>());
        _directionSelfControlLeverRepositoryMock
            .Setup(r => r.GetByNamesAsDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DirectionSelfControlLever>());
        _throwOutControlRepositoryMock.Setup(r => r.GetAllPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, ulong)>());

        var initializer = new ThrowOutControlDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _directionSelfControlLeverRepositoryMock.Object,
            _throwOutControlRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<ThrowOutControl>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
