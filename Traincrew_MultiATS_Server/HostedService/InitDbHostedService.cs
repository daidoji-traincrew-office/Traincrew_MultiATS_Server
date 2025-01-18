using System.Text.Json;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.HostedService;

public class InitDbHostedService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jsonstring = await File.ReadAllTextAsync("./Data/DBBase.json", cancellationToken);
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
                        IsShortCircuit = false,
                        TrainNumber = ""
                    }
                });
            }
            i++;
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;  // 何もしない
    }
}