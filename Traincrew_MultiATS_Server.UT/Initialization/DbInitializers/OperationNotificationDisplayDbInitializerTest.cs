using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.OperationNotificationDisplay;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class OperationNotificationDisplayDbInitializerTest
{
    private readonly Mock<ILogger<OperationNotificationDisplayDbInitializer>> _loggerMock = new();
    private readonly Mock<IDateTimeRepository> _dateTimeRepositoryMock = new();
    private readonly Mock<IOperationNotificationDisplayRepository> _operationNotificationDisplayRepositoryMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<ILogger<OperationNotificationDisplayCsvLoader>> _csvLoaderLoggerMock = new();
    private readonly Mock<OperationNotificationDisplayCsvLoader> _csvLoaderMock;
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    public OperationNotificationDisplayDbInitializerTest()
    {
        _csvLoaderMock = new(_csvLoaderLoggerMock.Object);
    }

    [Fact]
    [DisplayName("データが有効な場合、運転通告表示が正常に追加されること")]
    public async Task InitializeAsync_ShouldAddOperationNotificationDisplays_WhenDataIsValid()
    {
        // Arrange
        var now = DateTime.Now;
        var csvRecords = new List<OperationNotificationDisplayCsv>
        {
            new()
            {
                Name = "Display1",
                StationId = "ST1",
                IsUp = true,
                IsDown = false,
                TrackCircuitNames = ["TC1", "TC2"]
            }
        };

        var trackCircuits = new Dictionary<string, TrackCircuit>
        {
            { "TC1", new() { Id = 100, Name = "TC1" } },
            { "TC2", new() { Id = 200, Name = "TC2" } }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _trackCircuitRepositoryMock
            .Setup(r => r.GetByNames(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuits);
        _operationNotificationDisplayRepositoryMock
            .Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _dateTimeRepositoryMock.Setup(r => r.GetNow()).Returns(now);

        var initializer = new OperationNotificationDisplayDbInitializer(
            _loggerMock.Object,
            _dateTimeRepositoryMock.Object,
            _operationNotificationDisplayRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _csvLoaderMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<OperationNotificationDisplay>>(list =>
                list.Count == 1 &&
                list[0].Name == "Display1" &&
                list[0].StationId == "ST1" &&
                list[0].IsUp == true &&
                list[0].IsDown == false &&
                list[0].OperationNotificationState.DisplayName == "Display1" &&
                list[0].OperationNotificationState.Type == OperationNotificationType.None &&
                list[0].OperationNotificationState.Content == ""
            ), It.IsAny<CancellationToken>()),
            Times.Once);

        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<List<TrackCircuit>>(list =>
                list.Count == 2 &&
                list[0].OperationNotificationDisplayName == "Display1" &&
                list[1].OperationNotificationDisplayName == "Display1"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の運転通告表示が存在する場合、スキップされること")]
    public async Task InitializeAsync_ShouldSkipExistingDisplays()
    {
        // Arrange
        var now = DateTime.Now;
        var csvRecords = new List<OperationNotificationDisplayCsv>
        {
            new()
            {
                Name = "Display1",
                StationId = "ST1",
                IsUp = true,
                IsDown = false,
                TrackCircuitNames = []
            }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _trackCircuitRepositoryMock
            .Setup(r => r.GetByNames(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TrackCircuit>());
        _operationNotificationDisplayRepositoryMock
            .Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Display1"]);
        _dateTimeRepositoryMock.Setup(r => r.GetNow()).Returns(now);

        var initializer = new OperationNotificationDisplayDbInitializer(
            _loggerMock.Object,
            _dateTimeRepositoryMock.Object,
            _operationNotificationDisplayRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _csvLoaderMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<OperationNotificationDisplay>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("存在しない軌道回路が含まれる場合、スキップされること")]
    public async Task InitializeAsync_ShouldSkipNonExistentTrackCircuits()
    {
        // Arrange
        var now = DateTime.Now;
        var csvRecords = new List<OperationNotificationDisplayCsv>
        {
            new()
            {
                Name = "Display1",
                StationId = "ST1",
                IsUp = true,
                IsDown = false,
                TrackCircuitNames = ["TC1", "NonExistent", "TC2"]
            }
        };

        var trackCircuits = new Dictionary<string, TrackCircuit>
        {
            { "TC1", new() { Id = 100, Name = "TC1" } },
            { "TC2", new() { Id = 200, Name = "TC2" } }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _trackCircuitRepositoryMock
            .Setup(r => r.GetByNames(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackCircuits);
        _operationNotificationDisplayRepositoryMock
            .Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _dateTimeRepositoryMock.Setup(r => r.GetNow()).Returns(now);

        var initializer = new OperationNotificationDisplayDbInitializer(
            _loggerMock.Object,
            _dateTimeRepositoryMock.Object,
            _operationNotificationDisplayRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _csvLoaderMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<List<TrackCircuit>>(list =>
                list.Count == 2 // Only valid track circuits
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("操作日時が前日に設定されること")]
    public async Task InitializeAsync_ShouldSetOperatedAtToPreviousDay()
    {
        // Arrange
        var now = new DateTime(2025, 1, 15, 10, 0, 0);
        var expectedOperatedAt = now.AddDays(-1);

        var csvRecords = new List<OperationNotificationDisplayCsv>
        {
            new()
            {
                Name = "Display1",
                StationId = "ST1",
                IsUp = true,
                IsDown = false,
                TrackCircuitNames = []
            }
        };

        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(csvRecords);
        _trackCircuitRepositoryMock
            .Setup(r => r.GetByNames(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TrackCircuit>());
        _operationNotificationDisplayRepositoryMock
            .Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _dateTimeRepositoryMock.Setup(r => r.GetNow()).Returns(now);

        OperationNotificationDisplay? capturedDisplay = null;
        _generalRepositoryMock.Setup(r =>
                r.AddAll(It.IsAny<IEnumerable<OperationNotificationDisplay>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<OperationNotificationDisplay>, CancellationToken>((displays, _) =>
                capturedDisplay = displays.FirstOrDefault());

        var initializer = new OperationNotificationDisplayDbInitializer(
            _loggerMock.Object,
            _dateTimeRepositoryMock.Object,
            _operationNotificationDisplayRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _csvLoaderMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedDisplay);
        Assert.Equal(expectedOperatedAt, capturedDisplay.OperationNotificationState.OperatedAt);
    }

    [Fact]
    [DisplayName("CSVデータが空の場合、正常に処理されること")]
    public async Task InitializeAsync_ShouldHandleEmptyCsvData()
    {
        // Arrange
        _csvLoaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _trackCircuitRepositoryMock
            .Setup(r => r.GetByNames(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TrackCircuit>());
        _operationNotificationDisplayRepositoryMock
            .Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new OperationNotificationDisplayDbInitializer(
            _loggerMock.Object,
            _dateTimeRepositoryMock.Object,
            _operationNotificationDisplayRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _csvLoaderMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<OperationNotificationDisplay>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
