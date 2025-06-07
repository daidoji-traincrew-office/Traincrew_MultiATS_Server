using Traincrew_MultiATS_Server.IT.Fixture;

namespace Traincrew_MultiATS_Server.IT.Controller;

[Collection("PassengerWebApplication")]
public class PassengerControllerTest(PassengerWebApplicationFixture fixture)
{

    [Fact]
    public void CheckCanBootUp()
    {
        // 最低限、サーバーが起動するかどうか確認するだけなので、何もしない
    }
}