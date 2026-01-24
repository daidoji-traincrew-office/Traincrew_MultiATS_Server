using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitSignal;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class TrackCircuitDbInitializerTest
{
    private readonly Mock<ILogger<TrackCircuitDbInitializer>> _loggerMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<ISignalRepository> _signalRepositoryMock = new();
    private readonly Mock<ITrackCircuitSignalRepository> _trackCircuitSignalRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();
    private readonly Mock<ITrackCircuitDepartmentTimeRepository> _trackCircuitDepartmentTimeRepositoryMock = new();

    [Fact]
    [DisplayName("データが有効な場合、軌道回路が正常に追加されること")]
    public async Task InitializeTrackCircuitsAsync_ShouldAddTrackCircuits_WhenDataIsValid()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "TC1",
                ProtectionZone = 10,
                NextSignalNamesUp = [],
                NextSignalNamesDown = []
            },
            new()
            {
                Name = "TC2",
                ProtectionZone = null,
                NextSignalNamesUp = [],
                NextSignalNamesDown = []
            }
        };

        _trackCircuitRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act
        await initializer.InitializeTrackCircuitsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrackCircuit>>(list =>
                list.Count == 2 &&
                list[0].Name == "TC1" &&
                list[0].ProtectionZone == 10 &&
                list[0].Type == ObjectType.TrackCircuit &&
                list[0].TrackCircuitState.IsShortCircuit == false &&
                list[0].TrackCircuitState.IsLocked == false &&
                list[0].TrackCircuitState.TrainNumber == "" &&
                list[0].TrackCircuitState.IsCorrectionDropRelayRaised == RaiseDrop.Drop &&
                list[0].TrackCircuitState.IsCorrectionRaiseRelayRaised == RaiseDrop.Drop &&
                list[1].Name == "TC2" &&
                list[1].ProtectionZone == 99 // Default value
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の軌道回路が存在する場合、スキップされること")]
    public async Task InitializeTrackCircuitsAsync_ShouldSkipExistingTrackCircuits()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "TC1",
                ProtectionZone = 10,
                NextSignalNamesUp = [],
                NextSignalNamesDown = []
            }
        };

        _trackCircuitRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["TC1"]);

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act
        await initializer.InitializeTrackCircuitsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrackCircuit>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("防護区間のデフォルト値が正しく設定されること")]
    public async Task InitializeTrackCircuitsAsync_ShouldSetDefaultProtectionZone()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "TC1",
                ProtectionZone = null,
                NextSignalNamesUp = [],
                NextSignalNamesDown = []
            }
        };

        _trackCircuitRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act
        await initializer.InitializeTrackCircuitsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrackCircuit>>(list =>
                list.Count == 1 &&
                list[0].ProtectionZone == 99
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("データが有効な場合、軌道回路と信号機の関連が正常に追加されること")]
    public async Task InitializeTrackCircuitSignalsAsync_ShouldAddSignalRelationships_WhenDataIsValid()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "TC1",
                ProtectionZone = 10,
                NextSignalNamesUp = ["Signal1"],
                NextSignalNamesDown = ["Signal2"]
            }
        };

        var trackCircuitEntities = new Dictionary<string, TrackCircuit>
        {
            { "TC1", new() { Id = 100, Name = "TC1" } }
        };

        var signals = new Dictionary<string, Signal>
        {
            { "Signal1", new() { Name = "Signal1" } },
            { "Signal2", new() { Name = "Signal2" } }
        };

        _trackCircuitRepositoryMock
            .Setup(r => r.GetTrackCircuitsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitEntities);
        _signalRepositoryMock
            .Setup(r => r.GetSignalsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signals);
        _trackCircuitSignalRepositoryMock
            .Setup(r => r.GetExistingRelations(It.IsAny<HashSet<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, string)>());

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act
        await initializer.InitializeTrackCircuitSignalsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrackCircuitSignal>>(list =>
                list.Count == 2 &&
                list.Any(tcs => tcs.TrackCircuitId == 100 && tcs.SignalName == "Signal1" && tcs.IsUp == true) &&
                list.Any(tcs => tcs.TrackCircuitId == 100 && tcs.SignalName == "Signal2" && tcs.IsUp == false)
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の関連が存在する場合、スキップされること")]
    public async Task InitializeTrackCircuitSignalsAsync_ShouldSkipExistingRelationships()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "TC1",
                ProtectionZone = 10,
                NextSignalNamesUp = ["Signal1"],
                NextSignalNamesDown = []
            }
        };

        var trackCircuitEntities = new Dictionary<string, TrackCircuit>
        {
            { "TC1", new() { Id = 100, Name = "TC1" } }
        };

        var signals = new Dictionary<string, Signal>
        {
            { "Signal1", new() { Name = "Signal1" } }
        };

        var existingRelations = new HashSet<(ulong, string)> { (100, "Signal1") };

        _trackCircuitRepositoryMock
            .Setup(r => r.GetTrackCircuitsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitEntities);
        _signalRepositoryMock
            .Setup(r => r.GetSignalsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signals);
        _trackCircuitSignalRepositoryMock
            .Setup(r => r.GetExistingRelations(It.IsAny<HashSet<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRelations);

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act
        await initializer.InitializeTrackCircuitSignalsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrackCircuitSignal>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("存在しない軌道回路が含まれる場合、例外がスローされること")]
    public async Task InitializeTrackCircuitSignalsAsync_ShouldThrowException_WhenTrackCircuitNotFound()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "NonExistent",
                ProtectionZone = 10,
                NextSignalNamesUp = ["Signal1"],
                NextSignalNamesDown = []
            }
        };

        var trackCircuitEntities = new Dictionary<string, TrackCircuit>();
        var signals = new Dictionary<string, Signal>
        {
            { "Signal1", new() { Name = "Signal1" } }
        };

        _trackCircuitRepositoryMock
            .Setup(r => r.GetTrackCircuitsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitEntities);
        _signalRepositoryMock
            .Setup(r => r.GetSignalsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signals);
        _trackCircuitSignalRepositoryMock
            .Setup(r => r.GetExistingRelations(It.IsAny<HashSet<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, string)>());

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.InitializeTrackCircuitSignalsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken));

        Assert.Contains("NonExistent", exception.Message);
        Assert.Contains("軌道回路", exception.Message);
    }

    [Fact]
    [DisplayName("存在しない信号機が含まれる場合、例外がスローされること")]
    public async Task InitializeTrackCircuitSignalsAsync_ShouldThrowException_WhenSignalNotFound()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "TC1",
                ProtectionZone = 10,
                NextSignalNamesUp = ["NonExistentSignal"],
                NextSignalNamesDown = []
            }
        };

        var trackCircuitEntities = new Dictionary<string, TrackCircuit>
        {
            { "TC1", new() { Id = 100, Name = "TC1" } }
        };

        var signals = new Dictionary<string, Signal>();

        _trackCircuitRepositoryMock
            .Setup(r => r.GetTrackCircuitsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuitEntities);
        _signalRepositoryMock
            .Setup(r => r.GetSignalsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signals);
        _trackCircuitSignalRepositoryMock
            .Setup(r => r.GetExistingRelations(It.IsAny<HashSet<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(ulong, string)>());

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.InitializeTrackCircuitSignalsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken));

        Assert.Contains("NonExistentSignal", exception.Message);
        Assert.Contains("信号機", exception.Message);
    }

    [Fact]
    [DisplayName("初期化処理が何もせずに完了すること")]
    public async Task InitializeAsync_ShouldCompleteWithoutAction()
    {
        // Arrange
        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<List<TrackCircuit>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InitializeTrackCircuitsAsync_ShouldInitializeTrackCircuitStateCorrectly()
    {
        // Arrange
        var trackCircuitCsvList = new List<TrackCircuitCsv>
        {
            new()
            {
                Name = "TC1",
                ProtectionZone = 10,
                NextSignalNamesUp = [],
                NextSignalNamesDown = []
            }
        };

        _trackCircuitRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        TrackCircuit? capturedTrackCircuit = null;
        _generalRepositoryMock.Setup(r => r.AddAll(It.IsAny<IEnumerable<TrackCircuit>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TrackCircuit>, CancellationToken>((tcs, _) => capturedTrackCircuit = tcs.FirstOrDefault());

        var initializer = new TrackCircuitDbInitializer(
            _loggerMock.Object,
            _trackCircuitRepositoryMock.Object,
            _signalRepositoryMock.Object,
            _trackCircuitSignalRepositoryMock.Object,
            _generalRepositoryMock.Object,
            _trackCircuitDepartmentTimeRepositoryMock.Object);

        // Act
        await initializer.InitializeTrackCircuitsAsync(trackCircuitCsvList, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedTrackCircuit);
        Assert.NotNull(capturedTrackCircuit.TrackCircuitState);
        Assert.False(capturedTrackCircuit.TrackCircuitState.IsShortCircuit);
        Assert.False(capturedTrackCircuit.TrackCircuitState.IsLocked);
        Assert.Equal("", capturedTrackCircuit.TrackCircuitState.TrainNumber);
        Assert.Equal(RaiseDrop.Drop, capturedTrackCircuit.TrackCircuitState.IsCorrectionDropRelayRaised);
        Assert.Equal(RaiseDrop.Drop, capturedTrackCircuit.TrackCircuitState.IsCorrectionRaiseRelayRaised);
        Assert.Null(capturedTrackCircuit.TrackCircuitState.DroppedAt);
        Assert.Null(capturedTrackCircuit.TrackCircuitState.RaisedAt);
    }
}
