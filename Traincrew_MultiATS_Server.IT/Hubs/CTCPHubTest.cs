using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.Fixture;

namespace Traincrew_MultiATS_Server.IT.Hubs;

[Collection("WebApplication")]
public class CTCPHubTest(WebApplicationFixture factory)
{
    [Fact]
    public async Task CanConnectToCTCPHub()
    {
        // Arrange
        var mockClientContract = new Mock<ICTCPClientContract>();
        var (connection, _) = factory.CreateCTCPHub(mockClientContract.Object);

        // Act & Assert
        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HubConnectionState.Connected, connection.State);
        }
    }

    [Fact]
    public async Task SendData_CTCP_ReturnsValidData()
    {
        // Arrange
        var mockClientContract = new Mock<ICTCPClientContract>();
        var (connection, hub) = factory.CreateCTCPHub(mockClientContract.Object);

        DataToCTCP? result;
        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            // Act
            result = await hub.SendData_CTCP();
        }

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendData_CTCP_ReturnsRoutesOfStationsWithRouteState()
    {
        // Arrange
        var expectedTcNames = await GetTcNamesOfStationRoutesAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(expectedTcNames);

        var mockClientContract = new Mock<ICTCPClientContract>();
        var (connection, hub) = factory.CreateCTCPHub(mockClientContract.Object);

        DataToCTCP result;
        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            // Act
            result = await hub.SendData_CTCP();
        }

        // Assert
        Assert.Equal(expectedTcNames, result.Routes.Select(route => route.TcName).Order().ToList());
        Assert.All(result.Routes, route => Assert.NotNull(route.RouteState));
    }

    [Fact]
    public async Task ReceiveData_IncludesRoutesWithRouteState()
    {
        // Arrange
        var tcs = new TaskCompletionSource<DataToCTCP>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mockClientContract = new Mock<ICTCPClientContract>();
        mockClientContract
            .Setup(client => client.ReceiveData(It.IsAny<DataToCTCP>()))
            .Callback<DataToCTCP>(data => tcs.TrySetResult(data))
            .Returns(Task.CompletedTask);

        var (connection, _) = factory.CreateCTCPHub(mockClientContract.Object);

        DataToCTCP data;
        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            // Act
            data = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.NotEmpty(data.Routes);
        Assert.All(data.Routes, route => Assert.NotNull(route.RouteState));
    }

    [Fact]
    public async Task SetCtcRelay_IsReflectedInRouteStateOfSendData_CTCP()
    {
        // Arrange
        var mockClientContract = new Mock<ICTCPClientContract>();
        var (connection, hub) = factory.CreateCTCPHub(mockClientContract.Object);

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            var before = await hub.SendData_CTCP();
            // SetCtcRelay は TcName が一意な進路しか受け付けない
            var target = before.Routes
                .GroupBy(route => route.TcName)
                .Where(group => group.Count() == 1)
                .Select(group => group.Single())
                .First();
            var original = target.RouteState!.IsCtcControlled;
            var toggled = original == RaiseDrop.Raise ? RaiseDrop.Drop : RaiseDrop.Raise;

            try
            {
                // Act
                await hub.SetCtcRelay(target.TcName, toggled);
                var after = await hub.SendData_CTCP();

                // Assert
                var actual = Assert.Single(after.Routes, route => route.TcName == target.TcName);
                Assert.NotNull(actual.RouteState);
                Assert.Equal(toggled, actual.RouteState.IsCtcControlled);
            }
            finally
            {
                // 後続のテストに影響しないよう元に戻す
                await hub.SetCtcRelay(target.TcName, original);
            }
        }
    }

    /// <summary>
    /// 駅に属する進路の TcName を DB から直接取得する (CTCP に送られるべき進路の期待値)
    /// </summary>
    private async Task<List<string>> GetTcNamesOfStationRoutesAsync(CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stationIds = context.Stations
            .Where(station => station.IsStation)
            .Select(station => station.Id);
        var tcNames = await context.Routes
            .AsNoTracking()
            .Where(route => route.StationId != null && stationIds.Contains(route.StationId))
            .Select(route => route.TcName)
            .ToListAsync(ct);
        return tcNames.Order().ToList();
    }
}
