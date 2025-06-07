using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Traincrew_MultiATS_Server.Passenger;

namespace Traincrew_MultiATS_Server.IT.Fixture;

public class PassengerWebApplicationFixture
{
    private WebApplicationFactory<Program> factory = new();

    public PassengerWebApplicationFixture()
    {
        // Shift-JISを使用するために、CodePagesEncodingProviderを登録
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    internal HttpClient CreateClient()
    {
        var client = factory.CreateClient();

        return client;
    }

}
