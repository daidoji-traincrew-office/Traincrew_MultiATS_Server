using Microsoft.AspNetCore.SignalR.Client;
using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
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
}
