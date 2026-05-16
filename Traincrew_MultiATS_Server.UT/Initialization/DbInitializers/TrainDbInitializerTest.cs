using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;
using Traincrew_MultiATS_Server.Repositories.TrainType;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class TrainDbInitializerTest
{
    private readonly Mock<ILogger<TrainDbInitializer>> _loggerMock = new();
    private readonly Mock<ITrainTypeRepository> _trainTypeRepositoryMock = new();
    private readonly Mock<ITrainDiagramRepository> _trainDiagramRepositoryMock = new();
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
        var trainDiagramCsvList = new List<TrainDiagramCsv>
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
            r => r.AddAll(It.Is<List<TrainDiagram>>(list =>
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
        var trainDiagramCsvList = new List<TrainDiagramCsv>
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
            r => r.AddAll(It.Is<List<TrainDiagram>>(list =>
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
        var trainDiagramCsvList = new List<TrainDiagramCsv>();

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
            r => r.AddAll(It.Is<List<TrainDiagram>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
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
            r => r.AddAll(It.IsAny<List<TrainDiagram>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
