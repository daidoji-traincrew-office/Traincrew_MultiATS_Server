using Traincrew_MultiATS_Server.IT.Fixture;

namespace Traincrew_MultiATS_Server.IT.Controller;

[Collection("PassengerWebApplication")]
public class PassengerControllerTest(PassengerWebApplicationFixture fixture)
{

    [Fact]
    public void CheckCanBootUp()
    {
        fixture.CreateClient();
    }
}