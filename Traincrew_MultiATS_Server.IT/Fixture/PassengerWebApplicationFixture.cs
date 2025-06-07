using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Traincrew_MultiATS_Server.IT.Fixture;

public class PassengerWebApplicationFixture
{
    private WebApplicationFactory<Passenger.Program> factory = new();

    public PassengerWebApplicationFixture()
    {
        // Shift-JISを使用するために、CodePagesEncodingProviderを登録
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public WebApplicationFactory<Passenger.Program> Factory => factory;
}
