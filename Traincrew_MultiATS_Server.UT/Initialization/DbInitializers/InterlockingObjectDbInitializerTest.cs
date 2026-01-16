using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class InterlockingObjectDbInitializerTest
{
    private readonly Mock<ILogger<InterlockingObjectDbInitializer>> _loggerMock = new();
    private readonly Mock<IInterlockingObjectRepository> _interlockingObjectRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    [Fact]
    [DisplayName("オブジェクト名がパターンに一致する場合、駅IDが設定されること")]
    public async Task InitializeAsync_ShouldSetStationId_WhenNameMatchesPattern()
    {
        // Arrange
        var interlockingObjects = new List<InterlockingObject>
        {
            new Route { Id = 1, Name = "TH65_Route1", StationId = null },
            new Route { Id = 2, Name = "TH6S_Route2", StationId = null },
            new Route { Id = 3, Name = "TH123_Route3", StationId = null }
        };

        _interlockingObjectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(interlockingObjects);

        var initializer = new InterlockingObjectDbInitializer(
            _loggerMock.Object,
            _interlockingObjectRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<IEnumerable<InterlockingObject>>(list =>
                list.Count() == 2 &&
                list.ElementAt(0).StationId == "TH65" &&
                list.ElementAt(1).StationId == "TH6S"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("オブジェクト名がパターンに一致しない場合、スキップされること")]
    public async Task InitializeAsync_ShouldSkipObjects_WhenNameDoesNotMatchPattern()
    {
        // Arrange
        var interlockingObjects = new List<InterlockingObject>
        {
            new Route { Id = 1, Name = "InvalidName", StationId = null },
            new Route { Id = 2, Name = "TH65_Route", StationId = null },
            new Route { Id = 3, Name = "NoStationId", StationId = null }
        };

        _interlockingObjectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(interlockingObjects);

        var initializer = new InterlockingObjectDbInitializer(
            _loggerMock.Object,
            _interlockingObjectRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<IEnumerable<InterlockingObject>>(list =>
                list.Count() == 1 &&
                list.ElementAt(0).Name == "TH65_Route" &&
                list.ElementAt(0).StationId == "TH65"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("リストが空の場合、正常に処理されること")]
    public async Task InitializeAsync_ShouldHandleEmptyList()
    {
        // Arrange
        _interlockingObjectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new InterlockingObjectDbInitializer(
            _loggerMock.Object,
            _interlockingObjectRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<List<InterlockingObject>>(list => list.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("TH65_Signal", "TH65")]
    [InlineData("TH6S_Route", "TH6S")]
    [InlineData("TH1_Test", "TH1")]
    [InlineData("TH99_Object", "TH99")]
    [InlineData("TH12S_Item", "TH12S")]
    [DisplayName("オブジェクト名から正しく駅IDが抽出されること")]
    public async Task InitializeAsync_ShouldExtractCorrectStationId(string objectName, string expectedStationId)
    {
        // Arrange
        var interlockingObjects = new List<InterlockingObject>
        {
            new Route { Id = 1, Name = objectName, StationId = null }
        };

        _interlockingObjectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(interlockingObjects);

        var initializer = new InterlockingObjectDbInitializer(
            _loggerMock.Object,
            _interlockingObjectRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<IEnumerable<InterlockingObject>>(list =>
                list.Count() == 1 &&
                list.ElementAt(0).StationId == expectedStationId
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既に駅IDが設定されているオブジェクトも更新されること")]
    public async Task InitializeAsync_ShouldNotUpdateObjects_WithExistingStationId()
    {
        // Arrange
        var interlockingObjects = new List<InterlockingObject>
        {
            new Route { Id = 1, Name = "TH65_Route", StationId = "ExistingStation" }
        };

        _interlockingObjectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(interlockingObjects);

        var initializer = new InterlockingObjectDbInitializer(
            _loggerMock.Object,
            _interlockingObjectRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<IEnumerable<InterlockingObject>>(list =>
                list.Count() == 1 &&
                list.ElementAt(0).StationId == "TH65"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("有効なオブジェクトと無効なオブジェクトが混在する場合、正しく処理されること")]
    public async Task InitializeAsync_ShouldHandleMixedObjects()
    {
        // Arrange
        var interlockingObjects = new List<InterlockingObject>
        {
            new Route { Id = 1, Name = "TH65_Route1", StationId = null },
            new Route { Id = 2, Name = "InvalidName", StationId = null },
            new Route { Id = 3, Name = "TH66_Route2", StationId = null },
            new Route { Id = 4, Name = "NoMatch", StationId = null }
        };

        _interlockingObjectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(interlockingObjects);

        var initializer = new InterlockingObjectDbInitializer(
            _loggerMock.Object,
            _interlockingObjectRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.SaveAll(It.Is<IEnumerable<InterlockingObject>>(list =>
                list.Count() == 2 &&
                list.ElementAt(0).StationId == "TH65" &&
                list.ElementAt(1).StationId == "TH66"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
