using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Server;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class ServerStatusDbInitializerTest
{
    private readonly Mock<ILogger<ServerStatusDbInitializer>> _loggerMock = new();
    private readonly Mock<IServerRepository> _serverRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    [Fact]
    [DisplayName("サーバー状態が存在しない場合、新規に追加されること")]
    public async Task InitializeAsync_ShouldAddServerStatus_WhenNotExists()
    {
        // Arrange
        _serverRepositoryMock.Setup(r => r.GetServerStateAsync())
            .ReturnsAsync((ServerState?)null);

        var initializer = new ServerStatusDbInitializer(
            _loggerMock.Object,
            _serverRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.Add(It.Is<ServerState>(s =>
                s.Mode == ServerMode.Off &&
                s.TimeOffset == 0
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [DisplayName("サーバー状態が既に存在する場合、追加されないこと")]
    public async Task InitializeAsync_ShouldNotAddServerStatus_WhenAlreadyExists()
    {
        // Arrange
        var existingServerState = new ServerState
        {
            Mode = ServerMode.Public,
            TimeOffset = 100
        };

        _serverRepositoryMock.Setup(r => r.GetServerStateAsync())
            .ReturnsAsync(existingServerState);

        var initializer = new ServerStatusDbInitializer(
            _loggerMock.Object,
            _serverRepositoryMock.Object,
            _generalRepositoryMock.Object);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _generalRepositoryMock.Verify(
            r => r.Add(It.IsAny<ServerState>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [DisplayName("正しいデフォルト値が使用されること")]
    public async Task InitializeAsync_ShouldUseCorrectDefaultValues()
    {
        // Arrange
        _serverRepositoryMock.Setup(r => r.GetServerStateAsync())
            .ReturnsAsync((ServerState?)null);

        var initializer = new ServerStatusDbInitializer(
            _loggerMock.Object,
            _serverRepositoryMock.Object,
            _generalRepositoryMock.Object);

        ServerState? capturedState = null;
        _generalRepositoryMock.Setup(r => r.Add(It.IsAny<ServerState>(), It.IsAny<CancellationToken>()))
            .Callback<ServerState, CancellationToken>((state, _) => capturedState = state);

        // Act
        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(ServerMode.Off, capturedState.Mode);
        Assert.Equal(0, capturedState.TimeOffset);
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleCancellation()
    {
        // Arrange
        _serverRepositoryMock.Setup(r => r.GetServerStateAsync())
            .ReturnsAsync((ServerState?)null);

        var initializer = new ServerStatusDbInitializer(
            _loggerMock.Object,
            _serverRepositoryMock.Object,
            _generalRepositoryMock.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await initializer.InitializeAsync(cts.Token));
    }
}
