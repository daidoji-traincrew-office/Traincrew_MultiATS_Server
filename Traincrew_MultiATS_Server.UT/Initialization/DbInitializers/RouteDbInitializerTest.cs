using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class RouteDbInitializerTest
{
    private readonly Mock<ILogger<RouteLockTrackCircuitDbInitializer>> _loggerMock = new();
    private readonly Mock<ILogger<RouteLockTrackCircuitCsvLoader>> _csvLoaderLoggerMock = new();
    private readonly Mock<RouteLockTrackCircuitCsvLoader> _csvLoaderMock;
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<IRouteLockTrackCircuitRepository> _routeLockTrackCircuitRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    public RouteDbInitializerTest()
    {
        _csvLoaderMock = new(_csvLoaderLoggerMock.Object);
    }

    [Fact]
    [DisplayName("データが有効な場合、進路鎖錠軌道回路が正常に追加されること")]
    public async Task InitializeAsync_ShouldAddRouteLockTrackCircuits_WhenDataIsValid()
    {
        // Arrange
        var csvRecords = new List<RouteLockTrackCircuitCsv>
        {
            new()
            {
                RouteName = "Route1",
                TrackCircuitNames = ["TC1", "TC2"]
            }
        };

        var routeIdByName = new Dictionary<string, ulong> { { "Route1", 1 } };
        var trackCircuitIdByName = new Dictionary<string, ulong>
        {
            { "TC1", 100 },
            { "TC2", 200 }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>())).ReturnsAsync(routeIdByName);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);
        _routeLockTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new RouteLockTrackCircuitDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeLockTrackCircuitRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<RouteLockTrackCircuit>>(list =>
                list.Count == 2 &&
                list[0].RouteId == 1 &&
                list[0].TrackCircuitId == 100 &&
                list[1].RouteId == 1 &&
                list[1].TrackCircuitId == 200
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("進路が見つからない場合、例外がスローされること")]
    public async Task InitializeAsync_ShouldThrowException_WhenRouteNotFound()
    {
        // Arrange
        var csvRecords = new List<RouteLockTrackCircuitCsv>
        {
            new()
            {
                RouteName = "NonExistentRoute",
                TrackCircuitNames = ["TC1"]
            }
        };

        var routeIdByName = new Dictionary<string, ulong>();
        var trackCircuitIdByName = new Dictionary<string, ulong> { { "TC1", 100 } };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>())).ReturnsAsync(routeIdByName);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);
        _routeLockTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new RouteLockTrackCircuitDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeLockTrackCircuitRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.InitializeAsync(TestContext.Current.CancellationToken));

        Assert.Contains("NonExistentRoute", exception.Message);
        Assert.Contains("進路", exception.Message);
    }

    [Fact]
    [DisplayName("軌道回路が見つからない場合、例外がスローされること")]
    public async Task InitializeAsync_ShouldThrowException_WhenTrackCircuitNotFound()
    {
        // Arrange
        var csvRecords = new List<RouteLockTrackCircuitCsv>
        {
            new()
            {
                RouteName = "Route1",
                TrackCircuitNames = ["TC1", "NonExistentTC"]
            }
        };

        var routeIdByName = new Dictionary<string, ulong> { { "Route1", 1 } };
        var trackCircuitIdByName = new Dictionary<string, ulong> { { "TC1", 100 } };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>())).ReturnsAsync(routeIdByName);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);
        _routeLockTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new RouteLockTrackCircuitDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeLockTrackCircuitRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.InitializeAsync(TestContext.Current.CancellationToken));

        Assert.Contains("NonExistentTC", exception.Message);
        Assert.Contains("軌道回路", exception.Message);
    }

    [Fact]
    [DisplayName("既存のペアが存在する場合、スキップされること")]
    public async Task InitializeAsync_ShouldSkipExistingPairs()
    {
        // Arrange
        var csvRecords = new List<RouteLockTrackCircuitCsv>
        {
            new()
            {
                RouteName = "Route1",
                TrackCircuitNames = ["TC1", "TC2"]
            }
        };

        var routeIdByName = new Dictionary<string, ulong> { { "Route1", 1 } };
        var trackCircuitIdByName = new Dictionary<string, ulong>
        {
            { "TC1", 100 },
            { "TC2", 200 }
        };

        var existingPairs = new List<RouteLockTrackCircuit>
        {
            new() { RouteId = 1, TrackCircuitId = 100 }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>())).ReturnsAsync(routeIdByName);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);
        _routeLockTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPairs);

        var initializer = new RouteLockTrackCircuitDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeLockTrackCircuitRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<RouteLockTrackCircuit>>(list =>
                list.Count == 1 &&
                list[0].TrackCircuitId == 200
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("CSVデータが空の場合、正常に処理されること")]
    public async Task InitializeAsync_ShouldHandleEmptyCsvData()
    {
        // Arrange
        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _routeLockTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new RouteLockTrackCircuitDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeLockTrackCircuitRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<RouteLockTrackCircuit>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleMultipleRoutes()
    {
        // Arrange
        var csvRecords = new List<RouteLockTrackCircuitCsv>
        {
            new()
            {
                RouteName = "Route1",
                TrackCircuitNames = ["TC1"]
            },
            new()
            {
                RouteName = "Route2",
                TrackCircuitNames = ["TC2"]
            }
        };

        var routeIdByName = new Dictionary<string, ulong>
        {
            { "Route1", 1 },
            { "Route2", 2 }
        };
        var trackCircuitIdByName = new Dictionary<string, ulong>
        {
            { "TC1", 100 },
            { "TC2", 200 }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>())).ReturnsAsync(routeIdByName);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);
        _routeLockTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new RouteLockTrackCircuitDbInitializer(
            _loggerMock.Object,
            _csvLoaderMock.Object,
            _routeRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeLockTrackCircuitRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<RouteLockTrackCircuit>>(list =>
                list.Count == 2 &&
                list.Any(x => x.RouteId == 1 && x.TrackCircuitId == 100) &&
                list.Any(x => x.RouteId == 2 && x.TrackCircuitId == 200)
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
