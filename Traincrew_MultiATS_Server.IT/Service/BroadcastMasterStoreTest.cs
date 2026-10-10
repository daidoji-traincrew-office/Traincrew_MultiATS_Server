using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.IT.Service;

/// <summary>
/// 配信用マスタが実DBの内容で起動時にロードされていることのテスト。
/// </summary>
[Collection("WebApplication")]
public class BroadcastMasterStoreTest(WebApplicationFixture factory)
{
    [Fact(DisplayName = "起動完了時点でスナップショットがDBの内容で載っていること")]
    public async Task Startup_LoadsSnapshotFromDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = factory.Services.GetRequiredService<IBroadcastMasterStore>();

        // InitDbHostedService が連動オブジェクトのマスタの直後に読むので、この時点で例外にならない
        var master = store.Current;

        Assert.Equal(await context.Stations.CountAsync(station => station.IsStation, ct), master.StationIds.Count);
        Assert.Equal(await context.Signals.CountAsync(ct), master.Signals.Count);
        Assert.NotEmpty(master.Signals);
        Assert.All(master.Signals, signal => Assert.Equal(signal.TypeName, signal.Type.Name));
        Assert.Equal(
            await context.NextSignals.CountAsync(nextSignal => nextSignal.Depth == 1, ct),
            master.NextSignalNamesBySignalName.Values.Sum(names => names.Count));
        Assert.Equal(
            await context.SignalRoutes.CountAsync(ct),
            master.RouteIdsBySignalName.Values.Sum(routeIds => routeIds.Count));
        Assert.Equal(await context.TtcWindows.CountAsync(ct), master.TtcWindows.Count);
        Assert.Equal(
            await context.TtcWindowDisplayStations.CountAsync(ct),
            master.TtcWindows.Sum(window => window.DisplayStationIds.Count));
        Assert.Equal(await context.DestinationButtons.CountAsync(ct), master.DestinationButtons.Count);
    }
}
