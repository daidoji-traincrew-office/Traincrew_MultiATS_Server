using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class StationDbInitializerTest
{
    private readonly Mock<ILogger<StationDbInitializer>> _loggerMock = new();
    private readonly Mock<IStationRepository> _stationRepositoryMock = new();
    private readonly Mock<IStationTimerStateRepository> _stationTimerStateRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    [Fact]
    [DisplayName("データが有効な場合、駅が正常に追加されること")]
    public async Task InitializeStationsAsync_ShouldAddStations_WhenDataIsValid()
    {
        // Arrange
        var stationCsvList = new List<StationCsv>
        {
            new()
            {
                Id = "ST1",
                Name = "Station1",
                IsStation = true,
                IsPassengerStation = true
            },
            new()
            {
                Id = "ST2",
                Name = "Station2",
                IsStation = true,
                IsPassengerStation = false
            }
        };

        _stationRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeStationsAsync(stationCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<Station>>(list =>
                list.Count == 2 &&
                list[0].Id == "ST1" &&
                list[0].Name == "Station1" &&
                list[0].IsStation == true &&
                list[0].IsPassengerStation == true &&
                list[1].Id == "ST2" &&
                list[1].Name == "Station2"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の駅が存在する場合、スキップされること")]
    public async Task InitializeStationsAsync_ShouldSkipExistingStations()
    {
        // Arrange
        var stationCsvList = new List<StationCsv>
        {
            new()
            {
                Id = "ST1",
                Name = "Station1",
                IsStation = true,
                IsPassengerStation = true
            }
        };

        _stationRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Station1"]);

        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeStationsAsync(stationCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<Station>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("各駅に対してタイマー状態が正常に追加されること")]
    public async Task InitializeStationTimerStatesAsync_ShouldAddTimerStates_ForEachStation()
    {
        // Arrange
        var stationIds = new List<string> { "ST1", "ST2" };

        _stationRepositoryMock.Setup(r => r.GetIdsWhereIsStation(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stationIds);
        _stationTimerStateRepositoryMock.Setup(r => r.GetExistingTimerStates(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(string, int)>());

        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeStationTimerStatesAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<StationTimerState>>(list =>
                list.Count == 4 && // 2 stations * 2 timer types (30s, 60s)
                list.Count(s => s.StationId == "ST1" && s.Seconds == 30) == 1 &&
                list.Count(s => s.StationId == "ST1" && s.Seconds == 60) == 1 &&
                list.Count(s => s.StationId == "ST2" && s.Seconds == 30) == 1 &&
                list.Count(s => s.StationId == "ST2" && s.Seconds == 60) == 1 &&
                list.All(s => s.IsTeuRelayRaised == RaiseDrop.Drop) &&
                list.All(s => s.IsTenRelayRaised == RaiseDrop.Drop) &&
                list.All(s => s.IsTerRelayRaised == RaiseDrop.Raise)
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存のタイマー状態が存在する場合、スキップされること")]
    public async Task InitializeStationTimerStatesAsync_ShouldSkipExistingTimerStates()
    {
        // Arrange
        var stationIds = new List<string> { "ST1" };
        var existingTimerStates = new HashSet<(string, int)>
        {
            ("ST1", 30),
            ("ST1", 60)
        };

        _stationRepositoryMock.Setup(r => r.GetIdsWhereIsStation(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stationIds);
        _stationTimerStateRepositoryMock.Setup(r => r.GetExistingTimerStates(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTimerStates);

        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeStationTimerStatesAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<StationTimerState>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("一部のタイマー状態が存在する場合、不足分のみ追加されること")]
    public async Task InitializeStationTimerStatesAsync_ShouldAddPartialTimerStates()
    {
        // Arrange
        var stationIds = new List<string> { "ST1" };
        var existingTimerStates = new HashSet<(string, int)>
        {
            ("ST1", 30) // Only 30s exists, 60s should be added
        };

        _stationRepositoryMock.Setup(r => r.GetIdsWhereIsStation(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stationIds);
        _stationTimerStateRepositoryMock.Setup(r => r.GetExistingTimerStates(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTimerStates);

        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeStationTimerStatesAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<StationTimerState>>(list =>
                list.Count == 1 &&
                list[0].StationId == "ST1" &&
                list[0].Seconds == 60
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("駅リストが空の場合、正常に処理されること")]
    public async Task InitializeStationTimerStatesAsync_ShouldHandleEmptyStationList()
    {
        // Arrange
        _stationRepositoryMock.Setup(r => r.GetIdsWhereIsStation(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _stationTimerStateRepositoryMock.Setup(r => r.GetExistingTimerStates(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(string, int)>());

        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeStationTimerStatesAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<StationTimerState>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("初期化処理が何もせずに完了すること")]
    public async Task InitializeAsync_ShouldCompleteWithoutAction()
    {
        // Arrange
        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<List<Station>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<List<StationTimerState>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InitializeStationTimerStatesAsync_ShouldSetCorrectDefaultRelayStates()
    {
        // Arrange
        var stationIds = new List<string> { "ST1" };

        _stationRepositoryMock.Setup(r => r.GetIdsWhereIsStation(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stationIds);
        _stationTimerStateRepositoryMock.Setup(r => r.GetExistingTimerStates(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(string, int)>());

        StationTimerState? capturedState30 = null;
        StationTimerState? capturedState60 = null;

        _generalRepositoryMock.Setup(r => r.AddAll(It.IsAny<IEnumerable<StationTimerState>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<StationTimerState>, CancellationToken>((states, _) =>
            {
                capturedState30 = states.FirstOrDefault(s => s.Seconds == 30);
                capturedState60 = states.FirstOrDefault(s => s.Seconds == 60);
            });

        var initializer = new StationDbInitializer(
            _loggerMock.Object,
            _stationRepositoryMock.Object,
            _stationTimerStateRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeStationTimerStatesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedState30);
        Assert.NotNull(capturedState60);
        Assert.Equal(RaiseDrop.Drop, capturedState30.IsTeuRelayRaised);
        Assert.Equal(RaiseDrop.Drop, capturedState30.IsTenRelayRaised);
        Assert.Equal(RaiseDrop.Raise, capturedState30.IsTerRelayRaised);
        Assert.Equal(RaiseDrop.Drop, capturedState60.IsTeuRelayRaised);
        Assert.Equal(RaiseDrop.Drop, capturedState60.IsTenRelayRaised);
        Assert.Equal(RaiseDrop.Raise, capturedState60.IsTerRelayRaised);
    }
}
