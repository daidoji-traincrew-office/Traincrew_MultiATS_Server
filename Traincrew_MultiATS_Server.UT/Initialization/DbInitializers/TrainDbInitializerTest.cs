using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DiagramTrain;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.TrainType;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class TrainDbInitializerTest
{
    private readonly Mock<ILogger<TrainDbInitializer>> _loggerMock = new();
    private readonly Mock<ITrainTypeRepository> _trainTypeRepositoryMock = new();
    private readonly Mock<IDiagramTrainRepository> _trainDiagramRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    [Fact]
    [DisplayName("データが有効な場合、列車種別が正常に追加されること")]
    public async Task InitializeTrainTypesAsync_ShouldAddTrainTypes_WhenDataIsValid()
    {
        // Arrange
        var trainTypeCsvList = new List<TrainTypeCsv>
        {
            new() { Id = 1, Name = "Express" },
            new() { Id = 2, Name = "Local" }
        };

        _trainTypeRepositoryMock.Setup(r => r.GetIdsForAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new TrainDbInitializer(
            _loggerMock.Object,
            _trainTypeRepositoryMock.Object,
            _trainDiagramRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTrainTypesAsync(trainTypeCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrainType>>(list =>
                list.Count == 2 &&
                list[0].Id == 1 &&
                list[0].Name == "Express" &&
                list[1].Id == 2 &&
                list[1].Name == "Local"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の列車種別が存在する場合、スキップされること")]
    public async Task InitializeTrainTypesAsync_ShouldSkipExistingTrainTypes()
    {
        // Arrange
        var trainTypeCsvList = new List<TrainTypeCsv>
        {
            new() { Id = 1, Name = "Express" },
            new() { Id = 2, Name = "Local" }
        };

        _trainTypeRepositoryMock.Setup(r => r.GetIdsForAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([1]);

        var initializer = new TrainDbInitializer(
            _loggerMock.Object,
            _trainTypeRepositoryMock.Object,
            _trainDiagramRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTrainTypesAsync(trainTypeCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrainType>>(list =>
                list.Count == 1 &&
                list[0].Id == 2 &&
                list[0].Name == "Local"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("列車種別リストが空の場合、正常に処理されること")]
    public async Task InitializeTrainTypesAsync_ShouldHandleEmptyList()
    {
        // Arrange
        var trainTypeCsvList = new List<TrainTypeCsv>();

        _trainTypeRepositoryMock.Setup(r => r.GetIdsForAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new TrainDbInitializer(
            _loggerMock.Object,
            _trainTypeRepositoryMock.Object,
            _trainDiagramRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTrainTypesAsync(trainTypeCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<TrainType>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("データが有効な場合、列車ダイヤが正常に追加されること")]
    public async Task InitializeTrainDiagramsAsync_ShouldAddTrainDiagrams_WhenDataIsValid()
    {
        // Arrange
        var trainDiagramCsvList = new List<DiagramTrainCsv>
        {
            new()
            {
                TrainNumber = "101",
                TypeId = 1,
                FromStationId = "Station1",
                ToStationId = "Station2",
                DiaId = 1
            }
        };

        _trainDiagramRepositoryMock.Setup(r => r.GetTrainNumbersForAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new TrainDbInitializer(
            _loggerMock.Object,
            _trainTypeRepositoryMock.Object,
            _trainDiagramRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTrainDiagramsAsync(trainDiagramCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list =>
                list.Count == 1 &&
                list[0].TrainNumber == "101" &&
                list[0].TrainTypeId == 1 &&
                list[0].FromStationId == "Station1" &&
                list[0].ToStationId == "Station2" &&
                list[0].DiaId == 1
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の列車番号が存在する場合、スキップされること")]
    public async Task InitializeTrainDiagramsAsync_ShouldSkipExistingTrainNumbers()
    {
        // Arrange
        var trainDiagramCsvList = new List<DiagramTrainCsv>
        {
            new()
            {
                TrainNumber = "101",
                TypeId = 1,
                FromStationId = "Station1",
                ToStationId = "Station2",
                DiaId = 1
            },
            new()
            {
                TrainNumber = "102",
                TypeId = 2,
                FromStationId = "Station2",
                ToStationId = "Station3",
                DiaId = 2
            }
        };

        _trainDiagramRepositoryMock.Setup(r => r.GetTrainNumbersForAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["101"]);

        var initializer = new TrainDbInitializer(
            _loggerMock.Object,
            _trainTypeRepositoryMock.Object,
            _trainDiagramRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTrainDiagramsAsync(trainDiagramCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list =>
                list.Count == 1 &&
                list[0].TrainNumber == "102"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("列車ダイヤリストが空の場合、正常に処理されること")]
    public async Task InitializeTrainDiagramsAsync_ShouldHandleEmptyList()
    {
        // Arrange
        var trainDiagramCsvList = new List<DiagramTrainCsv>();

        _trainDiagramRepositoryMock.Setup(r => r.GetTrainNumbersForAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new TrainDbInitializer(
            _loggerMock.Object,
            _trainTypeRepositoryMock.Object,
            _trainDiagramRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeTrainDiagramsAsync(trainDiagramCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteWithoutAction()
    {
        // Arrange
        var initializer = new TrainDbInitializer(
            _loggerMock.Object,
            _trainTypeRepositoryMock.Object,
            _trainDiagramRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<List<TrainType>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<List<DiagramTrain>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private TrainDbInitializer CreateInitializer() =>
        new(_loggerMock.Object, _trainTypeRepositoryMock.Object, _trainDiagramRepositoryMock.Object, _generalRepositoryMock.Object);

    private TTC_Data CreateTtcData(string trainClass, string? trainName) => new()
    {
        TrainList =
        [
            new TTC_Train
            {
                trainNumber = "1",
                trainClass = trainClass,
                trainName = trainName,
                originStationID = "S1",
                destinationStationID = "S2",
                staList = []
            }
        ]
    };

    [Fact]
    [DisplayName("trainClassが辞書に存在する場合、対応するIDを使う")]
    public async Task InitializeFromTtcDataAsync_trainClassが辞書に存在する場合_対応するIDを使う()
    {
        // Arrange
        _trainTypeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, long> { ["普通"] = 5 });
        _trainDiagramRepositoryMock.Setup(r => r.GetForTrainNumberByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DiagramTrain>());
        _trainDiagramRepositoryMock.Setup(r => r.DeleteTimetablesByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeFromTtcDataAsync(CreateTtcData("普通", null), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list => list.Count == 1 && list[0].TrainTypeId == 5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("trainClassが辞書にない場合、デフォルト1を使う")]
    public async Task InitializeFromTtcDataAsync_trainClassが辞書にない場合_デフォルト1を使う()
    {
        // Arrange
        _trainTypeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, long>());
        _trainDiagramRepositoryMock.Setup(r => r.GetForTrainNumberByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DiagramTrain>());
        _trainDiagramRepositoryMock.Setup(r => r.DeleteTimetablesByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeFromTtcDataAsync(CreateTtcData("不明", null), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list => list.Count == 1 && list[0].TrainTypeId == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("trainClassが特急でtrainNameがnullの場合、デフォルト1を使う")]
    public async Task InitializeFromTtcDataAsync_trainClassが特急でtrainNameがnullの場合_デフォルト1を使う()
    {
        // Arrange
        _trainTypeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, long> { ["B特"] = 16 });
        _trainDiagramRepositoryMock.Setup(r => r.GetForTrainNumberByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DiagramTrain>());
        _trainDiagramRepositoryMock.Setup(r => r.DeleteTimetablesByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeFromTtcDataAsync(CreateTtcData("特急", null), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list => list.Count == 1 && list[0].TrainTypeId == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("trainClassが特急でtrainNameが辞書にない場合、デフォルト1を使う")]
    public async Task InitializeFromTtcDataAsync_trainClassが特急でtrainNameが辞書にない場合_デフォルト1を使う()
    {
        // Arrange
        _trainTypeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, long> { ["B特"] = 16 });
        _trainDiagramRepositoryMock.Setup(r => r.GetForTrainNumberByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DiagramTrain>());
        _trainDiagramRepositoryMock.Setup(r => r.DeleteTimetablesByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeFromTtcDataAsync(CreateTtcData("特急", "Ｚ特"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list => list.Count == 1 && list[0].TrainTypeId == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [DisplayName("trainClassが特急でtrainNameが全角の場合、半角変換したIDを使う")]
    [InlineData("Ａ特", "A特", 17)]
    [InlineData("Ｂ特", "B特", 16)]
    [InlineData("Ｃ特１", "C特1", 11)]
    [InlineData("Ｃ特２", "C特2", 12)]
    [InlineData("Ｃ特３", "C特3", 13)]
    [InlineData("Ｃ特４", "C特4", 14)]
    [InlineData("Ｃ特５", "C特5", 15)]
    public async Task InitializeFromTtcDataAsync_trainClassが特急でtrainNameが全角の場合_半角変換したIDを使う(
        string trainName, string halfWidthName, long expectedId)
    {
        // Arrange
        _trainTypeRepositoryMock.Setup(r => r.GetAllIdForName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, long> { [halfWidthName] = expectedId });
        _trainDiagramRepositoryMock.Setup(r => r.GetForTrainNumberByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, DiagramTrain>());
        _trainDiagramRepositoryMock.Setup(r => r.DeleteTimetablesByDiaId(It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeFromTtcDataAsync(CreateTtcData("特急", trainName), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<DiagramTrain>>(list => list.Count == 1 && list[0].TrainTypeId == expectedId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
