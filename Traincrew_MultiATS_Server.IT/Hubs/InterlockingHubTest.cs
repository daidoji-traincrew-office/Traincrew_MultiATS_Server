using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.Fixture;

namespace Traincrew_MultiATS_Server.IT.Hubs;

[Collection("WebApplication")]
public class InterlockingHubTest(WebApplicationFixture factory)
{
    [Fact]
    public async Task SendData_Interlocking_ReturnsValidDataForStation_TH76()
    {
        // Arrange
        var (connection, contract) = factory.CreateInterlockingHub();
        var activeStationsList = new List<string> { "TH76" };

        await using (connection)
        {
            await connection.StartAsync();

            // Act
            var result = await contract.SendData_Interlocking(activeStationsList);

            // Assert
            AssertValidDataForStation_TH76(result);
        }
    }

    private static void AssertValidDataForStation_TH76(DataToInterlocking data)
    {
        Assert.NotNull(data);
        Assert.NotNull(data.TrackCircuits);
        Assert.NotNull(data.Points);
        Assert.NotNull(data.Signals);
        Assert.NotNull(data.PhysicalLevers);
        Assert.NotNull(data.PhysicalKeyLevers);
        Assert.NotNull(data.PhysicalButtons);
        Assert.NotNull(data.Directions);
        Assert.NotNull(data.Retsubans);
        Assert.NotNull(data.Lamps);

        // Example: Check if at least one TrackCircuit exists
        Assert.NotEmpty(data.TrackCircuits);
    }
}