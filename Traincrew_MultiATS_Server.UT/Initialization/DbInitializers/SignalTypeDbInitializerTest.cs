using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.SignalType;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class SignalTypeDbInitializerTest
{
    private readonly Mock<ILogger<SignalTypeDbInitializer>> _loggerMock = new();
    private readonly Mock<ISignalTypeRepository> _signalTypeRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    [Fact]
    [DisplayName("データが有効な場合、信号機タイプが正常に追加されること")]
    public async Task InitializeSignalTypesAsync_ShouldAddSignalTypes_WhenDataIsValid()
    {
        // Arrange
        var signalTypeCsvList = new List<SignalTypeCsv>
        {
            new()
            {
                Name = "Type1",
                RIndication = "R",
                YYIndication = "YY",
                YIndication = "Y",
                YGIndication = "YG",
                GIndication = "G"
            }
        };

        _signalTypeRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new SignalTypeDbInitializer(
            _loggerMock.Object,
            _signalTypeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalTypesAsync(signalTypeCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SignalType>>(list =>
                list.Count == 1 &&
                list[0].Name == "Type1" &&
                list[0].RIndication == SignalIndication.R &&
                list[0].YYIndication == SignalIndication.YY &&
                list[0].YIndication == SignalIndication.Y &&
                list[0].YGIndication == SignalIndication.YG &&
                list[0].GIndication == SignalIndication.G
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("既存の信号機タイプが存在する場合、スキップされること")]
    public async Task InitializeSignalTypesAsync_ShouldSkipExistingSignalTypes()
    {
        // Arrange
        var signalTypeCsvList = new List<SignalTypeCsv>
        {
            new()
            {
                Name = "Type1",
                RIndication = "R",
                YYIndication = "YY",
                YIndication = "Y",
                YGIndication = "YG",
                GIndication = "G"
            }
        };

        _signalTypeRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Type1"]);

        var initializer = new SignalTypeDbInitializer(
            _loggerMock.Object,
            _signalTypeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalTypesAsync(signalTypeCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SignalType>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("無効な現示値が含まれる場合、デフォルト値で処理されること")]
    public async Task InitializeSignalTypesAsync_ShouldHandleInvalidIndications()
    {
        // Arrange
        var signalTypeCsvList = new List<SignalTypeCsv>
        {
            new()
            {
                Name = "Type1",
                RIndication = "INVALID",
                YYIndication = "INVALID",
                YIndication = "INVALID",
                YGIndication = "INVALID",
                GIndication = "INVALID"
            }
        };

        _signalTypeRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new SignalTypeDbInitializer(
            _loggerMock.Object,
            _signalTypeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalTypesAsync(signalTypeCsvList, TestContext.Current.CancellationToken);

        // Assert - Invalid indications should default to R
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SignalType>>(list =>
                list.Count == 1 &&
                list[0].RIndication == SignalIndication.R &&
                list[0].YYIndication == SignalIndication.R &&
                list[0].YIndication == SignalIndication.R &&
                list[0].YGIndication == SignalIndication.R &&
                list[0].GIndication == SignalIndication.R
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("リストが空の場合、正常に処理されること")]
    public async Task InitializeSignalTypesAsync_ShouldHandleEmptyList()
    {
        // Arrange
        var signalTypeCsvList = new List<SignalTypeCsv>();

        _signalTypeRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new SignalTypeDbInitializer(
            _loggerMock.Object,
            _signalTypeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalTypesAsync(signalTypeCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SignalType>>(list => list.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("初期化処理が何もせずに完了すること")]
    public async Task InitializeAsync_ShouldCompleteWithoutAction()
    {
        // Arrange
        var initializer = new SignalTypeDbInitializer(
            _loggerMock.Object,
            _signalTypeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<List<SignalType>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("R", SignalIndication.R)]
    [InlineData("YY", SignalIndication.YY)]
    [InlineData("Y", SignalIndication.Y)]
    [InlineData("YG", SignalIndication.YG)]
    [InlineData("G", SignalIndication.G)]
    public async Task InitializeSignalTypesAsync_ShouldMapIndicationsCorrectly(string input,
        SignalIndication expected)
    {
        // Arrange
        var signalTypeCsvList = new List<SignalTypeCsv>
        {
            new()
            {
                Name = "Type1",
                RIndication = input,
                YYIndication = input,
                YIndication = input,
                YGIndication = input,
                GIndication = input
            }
        };

        _signalTypeRepositoryMock.Setup(r => r.GetAllNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var initializer = new SignalTypeDbInitializer(
            _loggerMock.Object,
            _signalTypeRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeSignalTypesAsync(signalTypeCsvList, TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.Is<List<SignalType>>(list =>
                list.Count == 1 &&
                list[0].RIndication == expected &&
                list[0].YYIndication == expected &&
                list[0].YIndication == expected &&
                list[0].YGIndication == expected &&
                list[0].GIndication == expected
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
