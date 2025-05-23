using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.Fixture;

namespace Traincrew_MultiATS_Server.IT.Hubs;

[Collection("WebApplication")]
public class TIDHubTest(WebApplicationFixture factory)
{
    [Fact]
    public async Task ReceiveData_TID_ValidatesReceivedData()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ConstantDataToTID>();

        var mockClientContract = new Mock<ITIDClientContract>();
        mockClientContract
            .Setup(client => client.ReceiveData(It.IsAny<ConstantDataToTID>()))
            .Callback<ConstantDataToTID>(data =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(data);
                }
            })
            .Returns(Task.CompletedTask);

        var (connection, _) = factory.CreateTIDHub(mockClientContract.Object);

        ConstantDataToTID data;
        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);
            // Act
            data = await tcs.Task;
        }

        // Assert
        // Todo: 本当はTIDのCSVを持ってきて各要素があるかどうかを確認しないといけない
        Assert.NotEmpty(data.TrackCircuitDatas);
        Assert.NotEmpty(data.SwitchDatas);
        Assert.NotEmpty(data.DirectionDatas);
    }
}
