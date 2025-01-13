using System.Text.Json;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.HostedService;

// Todo: クラスの名前が良くない
public class TestHostedService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jsonstring = File.ReadAllText("./Data/DBBase.json");
        var DBBase = JsonSerializer.Deserialize<DBBasejson>(jsonstring);
        ulong i = 0;
        int protection_zone = 0;
        foreach (var item in DBBase.trackCircuitList)
        {
            // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
            if (!context.TrackCircuits.Any(tc => tc.Name == item.Name))
            {
                context.TrackCircuits.Add(new TrackCircuit
                {
                    ProtectionZone = protection_zone,
                    Name = item.Name,
                    Type = ObjectType.TrackCircuit,
                    TrackCircuitState = new TrackCircuitState
                    {
                        IsShortCircuit = false
                    }
                });
            }

            i++;
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}