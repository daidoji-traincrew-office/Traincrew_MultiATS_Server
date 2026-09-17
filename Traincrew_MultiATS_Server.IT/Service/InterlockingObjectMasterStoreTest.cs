using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.IT.Service;

/// <summary>
/// マスタのメモリ常駐が実DBに対して成立していることのテスト。
/// UT はスナップショットの引き当て規則を、ここでは「実DBから正しく載り、
/// 単表クエリ+メモリ結合が従来のJOINと同じ結果を返すこと」を見る。
/// </summary>
[Collection("WebApplication")]
public class InterlockingObjectMasterStoreTest(WebApplicationFixture factory)
{
    [Fact(DisplayName = "起動完了時点でスナップショットがDBの内容で載っていること")]
    public async Task Startup_LoadsSnapshotFromDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = factory.Services.GetRequiredService<IInterlockingObjectMasterStore>();

        // InitDbHostedService がオーケストレータ完走直後に読むので、この時点で例外にならない
        var master = store.Current;

        Assert.Equal(await context.InterlockingObjects.CountAsync(ct), master.All.Count);
        // TPTの派生表が具体型として載っていること(基底型のまま載ると引き当てが全部空振りする)
        Assert.Equal(
            await context.TrackCircuits.CountAsync(ct),
            master.All.OfType<TrackCircuit>().Count());
        Assert.NotEmpty(master.All.OfType<TrackCircuit>());
    }

    [Fact(DisplayName = "単表クエリ+メモリ結合が従来のJOINと同じ結果を返すこと")]
    public async Task GetTrackCircuitsByNames_MatchesJoinQuery()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var trackCircuitService = scope.ServiceProvider.GetRequiredService<ITrackCircuitService>();

        var names = await context.TrackCircuits
            .OrderBy(tc => tc.Id)
            .Select(tc => tc.Name)
            .Take(5)
            .ToListAsync(ct);
        // 重複名と存在しない名前を混ぜて、畳み込みと黙殺の挙動もまとめて見る
        var requested = names.Concat([names[0], "存在しない軌道回路"]).ToList();

        var actual = await trackCircuitService.GetTrackCircuitsByNames(requested);

        // 移行前の経路と同じ形のクエリを、このテストの中でオラクルとして組む
        var expected = await context.TrackCircuits
            .Include(tc => tc.TrackCircuitState)
            .Where(tc => requested.Contains(tc.Name))
            .ToListAsync(ct);

        Assert.Equal(
            expected.Select(Key).OrderBy(k => k.Id),
            actual.Select(Key).OrderBy(k => k.Id));
        return;

        static (ulong Id, string Name, int ProtectionZone, string? TrainNumber, bool On, bool Lock) Key(
            TrackCircuit tc) =>
            (tc.Id, tc.Name, tc.ProtectionZone, tc.TrackCircuitState.TrainNumber,
                tc.TrackCircuitState.IsShortCircuit, tc.TrackCircuitState.IsLocked);
    }

    [Fact(DisplayName = "ReloadAsyncでDBに追加された行がスナップショットへ反映されること")]
    public async Task ReloadAsync_PicksUpRowAddedAfterStartup()
    {
        var ct = TestContext.Current.CancellationToken;
        const string name = "IT_MASTER_RELOAD_TEST";
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = factory.Services.GetRequiredService<IInterlockingObjectMasterStore>();

        // 叩く前は見えない
        Assert.False(store.Current.TryGetByName<InterlockingObject>(name, out _));

        var added = new InterlockingObject { Name = name, Type = ObjectType.TrackCircuit };
        context.InterlockingObjects.Add(added);
        await context.SaveChangesAsync(ct);
        try
        {
            await store.ReloadAsync(ct);

            Assert.True(store.Current.TryGetByName<InterlockingObject>(name, out var reloaded));
            Assert.Equal(added.Id, reloaded.Id);
        }
        finally
        {
            // 後続テストが古い行を引かないよう、行を消してスナップショットも戻す
            context.InterlockingObjects.Remove(added);
            await context.SaveChangesAsync(ct);
            await store.ReloadAsync(ct);
        }
    }
}
