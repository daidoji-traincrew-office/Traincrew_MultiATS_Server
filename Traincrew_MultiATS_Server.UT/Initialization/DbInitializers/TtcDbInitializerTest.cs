using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcPath;
using Traincrew_MultiATS_Server.Repositories.TtcPathApproachTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcPathPredecessor;
using Traincrew_MultiATS_Server.Repositories.TtcPathRoute;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLinkRouteCondition;
using Traincrew_MultiATS_Server.Repositories.TtcWindowTrackCircuit;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class TtcDbInitializerTest
{
    private readonly Mock<ILogger<TtcDbInitializer>> _loggerMock = new();
    private readonly Mock<ILogger<TtcPathCsvLoader>> _pathCsvLoaderLoggerMock = new();
    private readonly Mock<ILogger<TtcWindowCsvLoader>> _windowCsvLoaderLoggerMock = new();
    private readonly Mock<ILogger<TtcWindowLinkCsvLoader>> _windowLinkCsvLoaderLoggerMock = new();
    private readonly Mock<TtcPathCsvLoader> _pathCsvLoaderMock;
    private readonly Mock<TtcWindowCsvLoader> _windowCsvLoaderMock;
    private readonly Mock<TtcWindowLinkCsvLoader> _windowLinkCsvLoaderMock;
    private readonly Mock<ITtcPathRepository> _ttcPathRepositoryMock = new();
    private readonly Mock<ITtcPathRouteRepository> _ttcPathRouteRepositoryMock = new();
    private readonly Mock<ITtcPathApproachTrackCircuitRepository> _ttcPathApproachTrackCircuitRepositoryMock = new();
    private readonly Mock<ITtcPathPredecessorRepository> _ttcPathPredecessorRepositoryMock = new();
    private readonly Mock<ITtcWindowRepository> _ttcWindowRepositoryMock = new();
    private readonly Mock<ITtcWindowTrackCircuitRepository> _ttcWindowTrackCircuitRepositoryMock = new();
    private readonly Mock<ITtcWindowLinkRepository> _ttcWindowLinkRepositoryMock = new();
    private readonly Mock<ITtcWindowLinkRouteConditionRepository> _ttcWindowLinkRouteConditionRepositoryMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    public TtcDbInitializerTest()
    {
        _pathCsvLoaderMock = new(_pathCsvLoaderLoggerMock.Object);
        _windowCsvLoaderMock = new(_windowCsvLoaderLoggerMock.Object);
        _windowLinkCsvLoaderMock = new(_windowLinkCsvLoaderLoggerMock.Object);
    }

    private TtcDbInitializer CreateInitializer()
    {
        return new TtcDbInitializer(
            _loggerMock.Object,
            _pathCsvLoaderMock.Object,
            _windowCsvLoaderMock.Object,
            _windowLinkCsvLoaderMock.Object,
            _ttcPathRepositoryMock.Object,
            _ttcPathRouteRepositoryMock.Object,
            _ttcPathApproachTrackCircuitRepositoryMock.Object,
            _ttcPathPredecessorRepositoryMock.Object,
            _ttcWindowRepositoryMock.Object,
            _ttcWindowLinkRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _routeRepositoryMock.Object,
            _generalRepositoryMock.Object);
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

        _ttcWindowRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);

        var initializer = CreateInitializer();

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
    [DisplayName("TTC窓の追加時、番線が設定されること(空欄は空文字)")]
    public async Task InitializeTtcWindowsAsync_ShouldSetTrackNumber()
    {
        // Arrange
        var csvRecords = new List<TtcWindowCsv>
        {
            new()
            {
                Name = "Window1", StationId = "Station1", TrackNumber = "1",
                Type = TtcWindowType.HomeTrack
            },
            new()
            {
                Name = "Window2", StationId = "Station1", TrackNumber = "",
                Type = TtcWindowType.Up
            }
        };

        _ttcWindowRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeTtcWindowsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindow>>(list =>
                list.Count == 2 &&
                list[0].TrackNumber == "1" &&
                list[1].TrackNumber == ""
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static TtcWindow Window(string name, string trackNumber) => new()
    {
        Name = name, StationId = "Station1", Type = TtcWindowType.HomeTrack, TrackNumber = trackNumber
    };

    private static TtcWindowCsv Csv(string name, string trackNumber) => new()
    {
        Name = name, StationId = "Station1", Type = TtcWindowType.HomeTrack, TrackNumber = trackNumber
    };

    private void SetupExistingWindows(params TtcWindow[] windows)
    {
        _ttcWindowRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(windows.ToList());
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
    }

    // SaveAll<TtcWindow> 呼び出し時点の番線を記録する(同じインスタンスを書き換えるため)
    private List<string> CaptureWindowSaves(List<string> calls)
    {
        _generalRepositoryMock
            .Setup(r => r.SaveAll(It.IsAny<List<TtcWindow>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TtcWindow>, CancellationToken>((list, _) =>
                calls.Add("Save:" + string.Join(",", list.Select(w => $"{w.Name}={w.TrackNumber}"))))
            .Returns(Task.CompletedTask);
        _generalRepositoryMock
            .Setup(r => r.AddAll(It.IsAny<List<TtcWindow>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TtcWindow>, CancellationToken>((_, _) => calls.Add("AddWindows"))
            .Returns(Task.CompletedTask);
        return calls;
    }

    [Fact]
    [DisplayName("既存のTTC窓の番線がCSVと違う場合、番線だけ更新されること")]
    public async Task InitializeTtcWindowsAsync_ShouldUpdateTrackNumber_WhenExistingWindowDiffers()
    {
        var existing = Window("Window1", "");
        var unchanged = Window("Window2", "2");
        SetupExistingWindows(existing, unchanged);
        var calls = CaptureWindowSaves([]);

        await CreateInitializer().InitializeTtcWindowsAsync(
            [Csv("Window1", "1"), Csv("Window2", "2")], TestContext.Current.CancellationToken);

        Assert.Equal("1", existing.TrackNumber);
        // いったん空文字で保存してから、目的の番線で保存する
        Assert.Equal(["Save:Window1=", "Save:Window1=1", "AddWindows"], calls);
    }

    [Fact]
    [DisplayName("既存のTTC窓の種類がCSVと違う場合、種類が更新されること")]
    public async Task InitializeTtcWindowsAsync_ShouldUpdateType_WhenExistingWindowDiffers()
    {
        var existing = Window("Window1", "5");
        SetupExistingWindows(existing);
        var record = Csv("Window1", "5");
        record.Type = TtcWindowType.SwitchingTrack;
        var calls = CaptureWindowSaves([]);

        await CreateInitializer().InitializeTtcWindowsAsync([record], TestContext.Current.CancellationToken);

        Assert.Equal(TtcWindowType.SwitchingTrack, existing.Type);
        Assert.Equal("5", existing.TrackNumber);
        Assert.Equal(["Save:Window1=", "Save:Window1=5", "AddWindows"], calls);
    }

    [Fact]
    [DisplayName("全てのTTC窓の番線がCSVと同じ場合、SaveAllが呼ばれないこと")]
    public async Task InitializeTtcWindowsAsync_ShouldNotSave_WhenTrackNumbersAreSame()
    {
        SetupExistingWindows(Window("Window1", "1"), Window("Window2", ""));

        await CreateInitializer().InitializeTtcWindowsAsync(
            [Csv("Window1", "1"), Csv("Window2", "")], TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.IsAny<List<TtcWindow>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [DisplayName("既存のTTC窓の番線を入れ替える場合、いったん空文字で保存してから目的の番線で保存すること")]
    public async Task InitializeTtcWindowsAsync_ShouldSwapTrackNumbersInTwoSteps()
    {
        var a = Window("WindowA", "1");
        var b = Window("WindowB", "2");
        SetupExistingWindows(a, b);
        var calls = CaptureWindowSaves([]);

        await CreateInitializer().InitializeTtcWindowsAsync(
            [Csv("WindowA", "2"), Csv("WindowB", "1")], TestContext.Current.CancellationToken);

        Assert.Equal("2", a.TrackNumber);
        Assert.Equal("1", b.TrackNumber);
        Assert.Equal(["Save:WindowA=,WindowB=", "Save:WindowA=2,WindowB=1", "AddWindows"], calls);
    }

    [Fact]
    [DisplayName("既存のTTC窓の番線更新では、表示駅・軌道回路が追加されないこと")]
    public async Task InitializeTtcWindowsAsync_ShouldNotAddChildren_WhenOnlyUpdatingTrackNumber()
    {
        SetupExistingWindows(Window("Window1", ""));
        var record = Csv("Window1", "1");
        record.DisplayStations = ["Station2"];
        record.TrackCircuits = ["TC1"];

        await CreateInitializer().InitializeTtcWindowsAsync([record], TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowDisplayStation>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowTrackCircuit>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("CSV内で同じ駅の番線が重複している場合、窓名を含む例外になること")]
    public async Task InitializeTtcWindowsAsync_ShouldThrow_WhenTrackNumberDuplicatedInCsv()
    {
        SetupExistingWindows();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateInitializer().InitializeTtcWindowsAsync(
                [Csv("WindowA", "1"), Csv("WindowB", "1")], TestContext.Current.CancellationToken));

        Assert.Contains("WindowA", exception.Message);
        Assert.Contains("WindowB", exception.Message);
    }

    [Fact]
    [DisplayName("番線が空文字の窓は、同じ駅に複数あっても例外にならないこと")]
    public async Task InitializeTtcWindowsAsync_ShouldNotThrow_WhenEmptyTrackNumbersDuplicated()
    {
        SetupExistingWindows();

        await CreateInitializer().InitializeTtcWindowsAsync(
            [Csv("WindowA", ""), Csv("WindowB", "")], TestContext.Current.CancellationToken);
    }

    [Fact]
    [DisplayName("CSVに無い既存窓は、番線が空文字になること")]
    public async Task InitializeTtcWindowsAsync_ShouldClearTrackNumber_WhenWindowNotInCsv()
    {
        var removed = Window("Removed", "1");
        SetupExistingWindows(removed, Window("Window2", "2"));
        var calls = CaptureWindowSaves([]);

        await CreateInitializer().InitializeTtcWindowsAsync([Csv("Window2", "2")],
            TestContext.Current.CancellationToken);

        Assert.Equal("", removed.TrackNumber);
        Assert.Equal(["Save:Removed=", "AddWindows"], calls);
    }

    [Fact]
    [DisplayName("CSVに無い既存窓の番線が既に空文字なら、更新しないこと")]
    public async Task InitializeTtcWindowsAsync_ShouldNotSave_WhenWindowNotInCsvHasEmptyTrackNumber()
    {
        SetupExistingWindows(Window("Removed", ""));

        await CreateInitializer().InitializeTtcWindowsAsync([], TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.IsAny<List<TtcWindow>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [DisplayName("消えた窓の番線を別の既存窓へ付け替える場合、先に空文字にしてから付け替えること")]
    public async Task InitializeTtcWindowsAsync_ShouldReassignTrackNumber_FromRemovedToExistingWindow()
    {
        var removed = Window("Removed", "1");
        var other = Window("Other", "");
        SetupExistingWindows(removed, other);
        var calls = CaptureWindowSaves([]);

        await CreateInitializer().InitializeTtcWindowsAsync([Csv("Other", "1")],
            TestContext.Current.CancellationToken);

        Assert.Equal("", removed.TrackNumber);
        Assert.Equal("1", other.TrackNumber);
        Assert.Equal(["Save:Removed=,Other=", "Save:Other=1", "AddWindows"], calls);
    }

    [Fact]
    [DisplayName("消えた窓の番線を新規窓へ付け替える場合、新規窓の追加より先に空文字にすること")]
    public async Task InitializeTtcWindowsAsync_ShouldReassignTrackNumber_FromRemovedToNewWindow()
    {
        var removed = Window("Removed", "1");
        SetupExistingWindows(removed);
        var calls = CaptureWindowSaves([]);

        await CreateInitializer().InitializeTtcWindowsAsync([Csv("New", "1")],
            TestContext.Current.CancellationToken);

        Assert.Equal("", removed.TrackNumber);
        Assert.Equal(["Save:Removed=", "AddWindows"], calls);
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

        _ttcWindowRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TtcWindow { Name = "Window1", StationId = "Station1", Type = TtcWindowType.HomeTrack }]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = CreateInitializer();

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
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitIdByName);

        var initializer = CreateInitializer();

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
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = CreateInitializer();

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
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLinkRouteCondition>>(list =>
                list.Count == 2 &&
                list[0].RouteId == 100 &&
                list[0].TtcWindowLinkId == 1 &&
                list[1].RouteId == 200 &&
                list[1].TtcWindowLinkId == 1
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
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcWindowLinkRouteCondition>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("リンクが存在しない場合、例外がスローされること")]
    public async Task InitializeTtcWindowLinkRouteConditionsAsync_ShouldThrowException_WhenLinkDoesNotExist()
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
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = CreateInitializer();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken));

        Assert.Contains("Window1", exception.Message);
        Assert.Contains("Window2", exception.Message);
        Assert.Contains("TTC窓リンク", exception.Message);
    }

    [Fact]
    [DisplayName("ルートが存在しない場合、例外がスローされること")]
    public async Task InitializeTtcWindowLinkRouteConditionsAsync_ShouldThrowException_WhenRouteDoesNotExist()
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
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeIdByName);

        var initializer = CreateInitializer();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.InitializeTtcWindowLinkRouteConditionsAsync(csvRecords, TestContext.Current.CancellationToken));

        Assert.Contains("NonExistentRoute", exception.Message);
        Assert.Contains("進路", exception.Message);
    }

    #region TTC進路

    private static TtcPathCsv PathCsv(
        string name,
        List<string>? routes = null,
        List<string>? approach = null,
        List<string>? predecessor = null,
        string station = "TH58")
    {
        return new TtcPathCsv
        {
            Name = name,
            Station = station,
            SourceTrack = "1番",
            TargetTrack = "2番",
            Type = TtcPathType.Departure,
            TargetRoutes = routes ?? [],
            DefineOrderTrackCircuitCount = 1,
            ApproachTrackCircuits = approach ?? [],
            Predecessor = predecessor ?? []
        };
    }

    private static TtcPath PathEntity(ulong id, string name, string station = "TH58", string source = "1番",
        string target = "2番", TtcPathType type = TtcPathType.Departure, int count = 1)
    {
        return new TtcPath
        {
            Id = id,
            Name = name,
            StationId = station,
            SourceTrack = source,
            TargetTrack = target,
            Type = type,
            DefineOrderTrackCircuitCount = count
        };
    }

    private void SetupPaths(params TtcPath[] paths)
    {
        _ttcPathRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(paths.ToList());
    }

    private void VerifyNoWrites<T>()
    {
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()), Times.Never);
        _generalRepositoryMock.Verify(
            r => r.DeleteAll(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [DisplayName("TTC進路が存在しない場合、新規追加されること")]
    public async Task InitializeTtcPathAsync_ShouldAddNewPaths()
    {
        _ttcPathRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathAsync([PathCsv("P1")], TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcPath>>(l => l.Count == 1 && l[0].Name == "P1" && l[0].StationId == "TH58"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.IsAny<List<TtcPath>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [DisplayName("TTC進路が変更なしの場合、何も書き込まれないこと")]
    public async Task InitializeTtcPathAsync_ShouldSkip_WhenUnchanged()
    {
        SetupPaths(PathEntity(1, "P1"));
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathAsync([PathCsv("P1")], TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<List<TtcPath>>(), It.IsAny<CancellationToken>()), Times.Never);
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.IsAny<List<TtcPath>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [DisplayName("TTC進路の内容が変わった場合、更新されること")]
    public async Task InitializeTtcPathAsync_ShouldUpdate_WhenChanged()
    {
        var existing = PathEntity(1, "P1", station: "OLD");
        SetupPaths(existing);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathAsync([PathCsv("P1", station: "TH58")],
            TestContext.Current.CancellationToken);

        Assert.Equal("TH58", existing.StationId);
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<List<TtcPath>>(l => l.Count == 1 && l[0] == existing),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- 対応進路 ----

    [Fact]
    [DisplayName("対応進路が新規の場合、indexが0始まりのCSV順で追加されること")]
    public async Task InitializeTtcPathRoutesAsync_ShouldAddWithIndex_WhenNew()
    {
        SetupPaths(PathEntity(10, "P1"));
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong> { ["R1"] = 100, ["R2"] = 200 });
        _ttcPathRouteRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathRoutesAsync([PathCsv("P1", routes: ["R2", "R1"])],
            TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcPathRoute>>(l =>
                l.Count == 2 &&
                l[0].TtcPathId == 10 && l[0].Index == 0 && l[0].RouteId == 200 &&
                l[1].TtcPathId == 10 && l[1].Index == 1 && l[1].RouteId == 100),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _generalRepositoryMock.Verify(
            r => r.DeleteAll(It.IsAny<IEnumerable<TtcPathRoute>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [DisplayName("対応進路が既存と一致する場合、追加も削除もされないこと")]
    public async Task InitializeTtcPathRoutesAsync_ShouldDoNothing_WhenUnchanged()
    {
        SetupPaths(PathEntity(10, "P1"));
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong> { ["R1"] = 100, ["R2"] = 200 });
        _ttcPathRouteRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TtcPathRoute { Id = 1, TtcPathId = 10, Index = 1, RouteId = 200 },
                new TtcPathRoute { Id = 2, TtcPathId = 10, Index = 0, RouteId = 100 }
            ]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathRoutesAsync([PathCsv("P1", routes: ["R1", "R2"])],
            TestContext.Current.CancellationToken);

        VerifyNoWrites<TtcPathRoute>();
    }

    [Fact]
    [DisplayName("対応進路の順序が変わった場合、そのパスの既存行が削除され作り直されること")]
    public async Task InitializeTtcPathRoutesAsync_ShouldReplace_WhenOrderChanged()
    {
        SetupPaths(PathEntity(10, "P1"), PathEntity(20, "P2"));
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong> { ["R1"] = 100, ["R2"] = 200, ["R3"] = 300 });
        var old0 = new TtcPathRoute { Id = 1, TtcPathId = 10, Index = 0, RouteId = 100 };
        var old1 = new TtcPathRoute { Id = 2, TtcPathId = 10, Index = 1, RouteId = 200 };
        var other = new TtcPathRoute { Id = 3, TtcPathId = 20, Index = 0, RouteId = 300 };
        _ttcPathRouteRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([old0, old1, other]);
        var calls = new List<string>();
        _generalRepositoryMock.Setup(r => r.DeleteAll(It.IsAny<IEnumerable<TtcPathRoute>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("Delete"))
            .Returns(Task.CompletedTask);
        _generalRepositoryMock.Setup(r => r.AddAll(It.IsAny<IEnumerable<TtcPathRoute>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("Add"))
            .Returns(Task.CompletedTask);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathRoutesAsync(
            [PathCsv("P1", routes: ["R2", "R1"]), PathCsv("P2", routes: ["R3"])],
            TestContext.Current.CancellationToken);

        Assert.Equal(["Delete", "Add"], calls);

        _generalRepositoryMock.Verify(
            r => r.DeleteAll(It.Is<List<TtcPathRoute>>(l => l.Count == 2 && l.Contains(old0) && l.Contains(old1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcPathRoute>>(l =>
                l.Count == 2 &&
                l[0].TtcPathId == 10 && l[0].Index == 0 && l[0].RouteId == 200 &&
                l[1].TtcPathId == 10 && l[1].Index == 1 && l[1].RouteId == 100),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("対応進路が未知の進路名の場合、例外がスローされること")]
    public async Task InitializeTtcPathRoutesAsync_ShouldThrow_WhenRouteUnknown()
    {
        SetupPaths(PathEntity(10, "P1"));
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _ttcPathRouteRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var initializer = CreateInitializer();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.InitializeTtcPathRoutesAsync([PathCsv("P1", routes: ["Unknown"])],
                TestContext.Current.CancellationToken));

        Assert.Contains("Unknown", ex.Message);
        Assert.Contains("P1", ex.Message);
        VerifyNoWrites<TtcPathRoute>();
    }

    [Fact]
    [DisplayName("CSVで対応進路が空になった場合、既存行の削除のみ行われること")]
    public async Task InitializeTtcPathRoutesAsync_ShouldOnlyDelete_WhenCsvBecomesEmpty()
    {
        SetupPaths(PathEntity(10, "P1"));
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong> { ["R1"] = 100 });
        var old = new TtcPathRoute { Id = 1, TtcPathId = 10, Index = 0, RouteId = 100 };
        _ttcPathRouteRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([old]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathRoutesAsync([PathCsv("P1", routes: [])],
            TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.DeleteAll(It.Is<List<TtcPathRoute>>(l => l.Count == 1 && l[0] == old),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<IEnumerable<TtcPathRoute>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [DisplayName("CSVのTTC進路名がttc_pathに存在しない場合、例外がスローされること")]
    public async Task InitializeTtcPathRoutesAsync_ShouldThrow_WhenPathNameNotInDb()
    {
        SetupPaths(PathEntity(10, "P1"));
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _ttcPathRouteRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var initializer = CreateInitializer();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.InitializeTtcPathRoutesAsync([PathCsv("MissingPath")],
                TestContext.Current.CancellationToken));

        Assert.Contains("MissingPath", ex.Message);
        Assert.Contains("TTC進路", ex.Message);
        VerifyNoWrites<TtcPathRoute>();
    }

    // ---- 接近判定 ----

    [Fact]
    [DisplayName("接近判定が新規の場合、indexが0始まりのCSV順で追加されること")]
    public async Task InitializeTtcPathApproachTrackCircuitsAsync_ShouldAddWithIndex_WhenNew()
    {
        SetupPaths(PathEntity(10, "P1"));
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong> { ["TC1"] = 100, ["TC2"] = 200 });
        _ttcPathApproachTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathApproachTrackCircuitsAsync(
            [PathCsv("P1", approach: ["TC2", "TC1"])], TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcPathApproachTrackCircuit>>(l =>
                l.Count == 2 &&
                l[0].TtcPathId == 10 && l[0].Index == 0 && l[0].TrackCircuitId == 200 &&
                l[1].TtcPathId == 10 && l[1].Index == 1 && l[1].TrackCircuitId == 100),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("接近判定が既存と一致する場合、追加も削除もされないこと")]
    public async Task InitializeTtcPathApproachTrackCircuitsAsync_ShouldDoNothing_WhenUnchanged()
    {
        SetupPaths(PathEntity(10, "P1"));
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong> { ["TC1"] = 100 });
        _ttcPathApproachTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TtcPathApproachTrackCircuit { Id = 1, TtcPathId = 10, Index = 0, TrackCircuitId = 100 }]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathApproachTrackCircuitsAsync(
            [PathCsv("P1", approach: ["TC1"])], TestContext.Current.CancellationToken);

        VerifyNoWrites<TtcPathApproachTrackCircuit>();
    }

    [Fact]
    [DisplayName("接近判定の参照先が変わった場合、そのパスの既存行が削除され作り直されること")]
    public async Task InitializeTtcPathApproachTrackCircuitsAsync_ShouldReplace_WhenChanged()
    {
        SetupPaths(PathEntity(10, "P1"));
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong> { ["TC1"] = 100, ["TC2"] = 200 });
        var old = new TtcPathApproachTrackCircuit { Id = 1, TtcPathId = 10, Index = 0, TrackCircuitId = 100 };
        _ttcPathApproachTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([old]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathApproachTrackCircuitsAsync(
            [PathCsv("P1", approach: ["TC2"])], TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.DeleteAll(It.Is<List<TtcPathApproachTrackCircuit>>(l => l.Count == 1 && l[0] == old),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcPathApproachTrackCircuit>>(l =>
                l.Count == 1 && l[0].Index == 0 && l[0].TrackCircuitId == 200),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("接近判定が未知の軌道回路名の場合、例外がスローされること")]
    public async Task InitializeTtcPathApproachTrackCircuitsAsync_ShouldThrow_WhenTrackCircuitUnknown()
    {
        SetupPaths(PathEntity(10, "P1"));
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _ttcPathApproachTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var initializer = CreateInitializer();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.InitializeTtcPathApproachTrackCircuitsAsync(
                [PathCsv("P1", approach: ["Unknown"])], TestContext.Current.CancellationToken));

        Assert.Contains("Unknown", ex.Message);
        Assert.Contains("P1", ex.Message);
        VerifyNoWrites<TtcPathApproachTrackCircuit>();
    }

    // ---- 近接前対応 ----

    [Fact]
    [DisplayName("近接前対応が新規の場合、indexが0始まりのCSV順で追加されること")]
    public async Task InitializeTtcPathPredecessorsAsync_ShouldAddWithIndex_WhenNew()
    {
        SetupPaths(PathEntity(10, "P1"), PathEntity(20, "P2"), PathEntity(30, "P3"));
        _ttcPathPredecessorRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathPredecessorsAsync(
            [PathCsv("P1", predecessor: ["P3", "P2"]), PathCsv("P2"), PathCsv("P3")],
            TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcPathPredecessor>>(l =>
                l.Count == 2 &&
                l[0].SourceTtcPathId == 10 && l[0].Index == 0 && l[0].TargetTtcPathId == 30 &&
                l[1].SourceTtcPathId == 10 && l[1].Index == 1 && l[1].TargetTtcPathId == 20),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("近接前対応が既存と一致する場合、追加も削除もされないこと")]
    public async Task InitializeTtcPathPredecessorsAsync_ShouldDoNothing_WhenUnchanged()
    {
        SetupPaths(PathEntity(10, "P1"), PathEntity(20, "P2"));
        _ttcPathPredecessorRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TtcPathPredecessor { Id = 1, SourceTtcPathId = 10, Index = 0, TargetTtcPathId = 20 }]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathPredecessorsAsync(
            [PathCsv("P1", predecessor: ["P2"]), PathCsv("P2")], TestContext.Current.CancellationToken);

        VerifyNoWrites<TtcPathPredecessor>();
    }

    [Fact]
    [DisplayName("近接前対応が変わった場合、そのパスの既存行が削除され作り直されること")]
    public async Task InitializeTtcPathPredecessorsAsync_ShouldReplace_WhenChanged()
    {
        SetupPaths(PathEntity(10, "P1"), PathEntity(20, "P2"), PathEntity(30, "P3"));
        var old = new TtcPathPredecessor { Id = 1, SourceTtcPathId = 10, Index = 0, TargetTtcPathId = 20 };
        _ttcPathPredecessorRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([old]);
        var initializer = CreateInitializer();

        await initializer.InitializeTtcPathPredecessorsAsync(
            [PathCsv("P1", predecessor: ["P3"]), PathCsv("P2"), PathCsv("P3")],
            TestContext.Current.CancellationToken);

        _generalRepositoryMock.Verify(
            r => r.DeleteAll(It.Is<List<TtcPathPredecessor>>(l => l.Count == 1 && l[0] == old),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TtcPathPredecessor>>(l =>
                l.Count == 1 && l[0].SourceTtcPathId == 10 && l[0].Index == 0 && l[0].TargetTtcPathId == 30),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("近接前対応が未知のTTC進路名の場合、例外がスローされること")]
    public async Task InitializeTtcPathPredecessorsAsync_ShouldThrow_WhenPathUnknown()
    {
        SetupPaths(PathEntity(10, "P1"));
        _ttcPathPredecessorRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var initializer = CreateInitializer();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.InitializeTtcPathPredecessorsAsync(
                [PathCsv("P1", predecessor: ["Unknown"])], TestContext.Current.CancellationToken));

        Assert.Contains("Unknown", ex.Message);
        Assert.Contains("P1", ex.Message);
        VerifyNoWrites<TtcPathPredecessor>();
    }

    #endregion


    [Fact]
    [DisplayName("InitializeAsync が全ローダーを呼び出し、TTC進路の子テーブル初期化まで実行すること")]
    public async Task InitializeAsync_ShouldLoadAllCsvAndInitializePathChildren()
    {
        // Arrange
        _pathCsvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([PathCsv("P1")]);
        _ttcPathRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([PathEntity(1, "P1")]);
        _ttcPathRouteRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ttcPathApproachTrackCircuitRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ttcPathPredecessorRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _windowCsvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _windowLinkCsvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ttcWindowRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllLinkPairsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(string, string)>());
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLink())
            .ReturnsAsync([]);
        _ttcWindowLinkRepositoryMock.Setup(r => r.GetAllTtcWindowLinkRouteConditions())
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());
        _routeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ulong>());

        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _pathCsvLoaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _ttcPathRouteRepositoryMock.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _ttcPathApproachTrackCircuitRepositoryMock.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _ttcPathPredecessorRepositoryMock.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _windowCsvLoaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _windowLinkCsvLoaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
