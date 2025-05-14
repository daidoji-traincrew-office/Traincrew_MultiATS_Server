using Traincrew_MultiATS_Server.IT.Fixture;

namespace Traincrew_MultiATS_Server.IT.Logic;

// ReSharper disable once InconsistentNaming
[Collection("WebApplication")]
public class LogicTest_TH71(WebApplicationFixture factory)
{
    [Fact]
    public async Task Get_Endpoint_ReturnsSuccess()
    {
        var (connection, contract) = factory.CreateTrainHub();
        await using (connection)
        {
            await connection.StartAsync();
        }
    }
}