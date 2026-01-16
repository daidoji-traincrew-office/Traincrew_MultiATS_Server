using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLinkRouteCondition;
using Traincrew_MultiATS_Server.Repositories.TtcWindowTrackCircuit;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class TtcDbInitializerTest
{
    private readonly Mock<ILogger<TtcDbInitializer>> _loggerMock = new();
    private readonly Mock<ILogger<TtcWindowCsvLoader>> _windowCsvLoaderLoggerMock = new();
    private readonly Mock<ILogger<TtcWindowLinkCsvLoader>> _windowLinkCsvLoaderLoggerMock = new();
    private readonly Mock<TtcWindowCsvLoader> _windowCsvLoaderMock;
    private readonly Mock<TtcWindowLinkCsvLoader> _windowLinkCsvLoaderMock;
    private readonly Mock<ITtcWindowRepository> _ttcWindowRepositoryMock = new();
    private readonly Mock<ITtcWindowTrackCircuitRepository> _ttcWindowTrackCircuitRepositoryMock = new();
    private readonly Mock<ITtcWindowLinkRepository> _ttcWindowLinkRepositoryMock = new();
    private readonly Mock<ITtcWindowLinkRouteConditionRepository> _ttcWindowLinkRouteConditionRepositoryMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    public TtcDbInitializerTest()
    {
        _windowCsvLoaderMock = new(_windowCsvLoaderLoggerMock.Object);
        _windowLinkCsvLoaderMock = new(_windowLinkCsvLoaderLoggerMock.Object);
    }

    [Fact]
    [DisplayName("データが有効な場合、TTC窓が正常に追加されること")]
    public async Task InitializeTtcWindowsAsync_ShouldAddWindows_WhenDataIsValid()
    {
        // Arrange
        var csvRecords = new List<TtcWindowCsv>
        {
            new()
            {
                Name = "Window1",
                StationId = "Station1",
                Type = TtcWindowType.HomeTrack,
                DisplayStations = ["Station1", "Station2"],
                TrackCircuits = ["TC1", "TC2"]
            }
        };

        var trackCircuitIdByName = new Dictionary<string, ulong>
        {
            { "TC1", 100 },
            { "TC2", 200 }
        };

        _ttcWindowRepositoryMock.Setup(r => r.GetAllWindowNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindow>>(list =>
                list.Count == 1 &&
                list[0].Name == "Window1" &&
                list[0].StationId == "Station1" &&
                list[0].Type == TtcWindowType.HomeTrack
            ), It.IsAny<CancellationToken>()),
            Times.Once);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowDisplayStation>>(list =>
                list.Count == 2 &&
                list[0].TtcWindowName == "Window1" &&
                list[0].StationId == "Station1"
            ), It.IsAny<CancellationToken>()),
            Times.Once);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowTrackCircuit>>(list =>
                list.Count == 2 &&
                list[0].TtcWindowName == "Window1" &&
                list[0].TrackCircuitId == 100
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存のTTC窓が存在する場合、スキップされること")]
    public async Task InitializeTtcWindowsAsync_ShouldSkipExistingWindows()
    {
        // Arrange
        var csvRecords = new List<TtcWindowCsv>
        {
            new()
            {
                Name = "Window1",
                StationId = "Station1",
                Type = TtcWindowType.HomeTrack,
                DisplayStations = [],
                TrackCircuits = []
            }
        };

        _ttcWindowRepositoryMock.Setup(r => r.GetAllWindowNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Window1"]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindow>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("データが有効な場合、TTC窓リンクが正常に追加されること")]
    public async Task InitializeTtcWindowLinksAsync_ShouldAddLinks_WhenDataIsValid()
    {
        // Arrange
        var csvRecords = new List<TtcWindowLinkCsv>
        {
            new()
            {
                Source = "Window1",
                Target = "Window2",
                Type = TtcWindowLinkType.Up,
                IsEmptySending = true,
                TrackCircuitCondition = "TC1",
                RouteConditions = ["Route1", "Route2"]
            }
        };

        var trackCircuitIdByName = new Dictionary<string, ulong> { { "TC1", 100 } };
        var routeIdByName = new Dictionary<string, ulong>
        {
            { "Route1", 1 },
            { "Route2", 2 }
        };

        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllLinkPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(string, string)>());
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowLinksAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLink>>(list =>
                list.Count == 1 &&
                list[0].SourceTtcWindowName == "Window1" &&
                list[0].TargetTtcWindowName == "Window2" &&
                list[0].Type == TtcWindowLinkType.Up &&
                list[0].IsEmptySending == true &&
                list[0].TrackCircuitCondition == 100
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存のTTC窓リンクが存在する場合、スキップされること")]
    public async Task InitializeTtcWindowLinksAsync_ShouldSkipExistingLinks()
    {
        // Arrange
        var csvRecords = new List<TtcWindowLinkCsv>
        {
            new()
            {
                Source = "Window1",
                Target = "Window2",
                Type = TtcWindowLinkType.Up,
                IsEmptySending = false,
                TrackCircuitCondition = null,
                RouteConditions = []
            }
        };

        var existingLinks = new HashSet<(string, string)> { ("Window1", "Window2") };

        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllLinkPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLinks);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowLinksAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLink>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("データが有効な場合、TTC窓リンクルート条件が正常に追加されること")]
    public async Task InitializeTtcWindowLinkRouteConditionsAsync_ShouldAddRouteConditions_WhenDataIsValid()
    {
        // Arrange
        var csvRecords = new List<TtcWindowLinkCsv>
        {
            new()
            {
                Source = "Window1",
                Target = "Window2",
                Type = TtcWindowLinkType.Up,
                IsEmptySending = true,
                TrackCircuitCondition = null,
                RouteConditions = ["Route1", "Route2"]
            }
        };

        var existingLink = new TtcWindowLink
        {
            Id = 1,
            SourceTtcWindowName = "Window1",
            TargetTtcWindowName = "Window2",
            Type = TtcWindowLinkType.Up,
            IsEmptySending = true
        };

        var routeIdByName = new Dictionary<string, ulong>
        {
            { "Route1", 100 },
            { "Route2", 200 }
        };

        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLink())
            .ReturnsAsync([existingLink]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLinkRouteConditions())
            .ReturnsAsync([]);
        _routeRepositoryMock.Setup(r => r.GetIdsByName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLinkRouteCondition>>(list =>
                list.Count == 2 &&
                list[0].RouteId == 100 &&
                list[0].TtcWindowLink.Id == 1 &&
                list[1].RouteId == 200 &&
                list[1].TtcWindowLink.Id == 1
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存のルート条件が存在する場合、スキップされること")]
    public async Task InitializeTtcWindowLinkRouteConditionsAsync_ShouldSkipExistingConditions()
    {
        // Arrange
        var csvRecords = new List<TtcWindowLinkCsv>
        {
            new()
            {
                Source = "Window1",
                Target = "Window2",
                Type = TtcWindowLinkType.Up,
                IsEmptySending = true,
                TrackCircuitCondition = null,
                RouteConditions = ["Route1"]
            }
        };

        var existingLink = new TtcWindowLink
        {
            Id = 1,
            SourceTtcWindowName = "Window1",
            TargetTtcWindowName = "Window2",
            Type = TtcWindowLinkType.Up,
            IsEmptySending = true
        };

        var existingCondition = new TtcWindowLinkRouteCondition
        {
            TtcWindowLinkId = 1,
            RouteId = 100
        };

        var routeIdByName = new Dictionary<string, ulong>
        {
            { "Route1", 100 }
        };

        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLink())
            .ReturnsAsync([existingLink]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLinkRouteConditions())
            .ReturnsAsync([existingCondition]);
        _routeRepositoryMock.Setup(r => r.GetIdsByName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLinkRouteCondition>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("リンクが存在しない場合、ルート条件がスキップされること")]
    public async Task InitializeTtcWindowLinkRouteConditionsAsync_ShouldSkipConditions_WhenLinkDoesNotExist()
    {
        // Arrange
        var csvRecords = new List<TtcWindowLinkCsv>
        {
            new()
            {
                Source = "Window1",
                Target = "Window2",
                Type = TtcWindowLinkType.Up,
                IsEmptySending = true,
                TrackCircuitCondition = null,
                RouteConditions = ["Route1"]
            }
        };

        var routeIdByName = new Dictionary<string, ulong>
        {
            { "Route1", 100 }
        };

        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLink())
            .ReturnsAsync([]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLinkRouteConditions())
            .ReturnsAsync([]);
        _routeRepositoryMock.Setup(r => r.GetIdsByName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLinkRouteCondition>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("ルートが存在しない場合、そのルート条件がスキップされること")]
    public async Task InitializeTtcWindowLinkRouteConditionsAsync_ShouldSkipConditions_WhenRouteDoesNotExist()
    {
        // Arrange
        var csvRecords = new List<TtcWindowLinkCsv>
        {
            new()
            {
                Source = "Window1",
                Target = "Window2",
                Type = TtcWindowLinkType.Up,
                IsEmptySending = true,
                TrackCircuitCondition = null,
                RouteConditions = ["Route1", "NonExistentRoute"]
            }
        };

        var existingLink = new TtcWindowLink
        {
            Id = 1,
            SourceTtcWindowName = "Window1",
            TargetTtcWindowName = "Window2",
            Type = TtcWindowLinkType.Up,
            IsEmptySending = true
        };

        var routeIdByName = new Dictionary<string, ulong>
        {
            { "Route1", 100 }
        };

        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLink())
            .ReturnsAsync([existingLink]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLinkRouteConditions())
            .ReturnsAsync([]);
        _routeRepositoryMock.Setup(r => r.GetIdsByName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLinkRouteCondition>>(list =>
                list.Count == 1 &&
                list[0].RouteId == 100
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCallBothInitializationMethods()
    {
        // Arrange
        _windowCsvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _windowLinkCsvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ttcWindowRepositoryMock.Setup(r => r.GetAllWindowNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllLinkPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(string, string)>());
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLink())
            .ReturnsAsync([]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLinkRouteConditions())
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdsForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _routeRepositoryMock.Setup(r => r.GetIdsByName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = new TtcDbInitializer(
            _loggerMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _windowCsvLoaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _windowLinkCsvLoaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
