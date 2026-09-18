using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.IT.Service;

/// <summary>
/// 告知器マスタのメモリ常駐が実DBに対して成立していることのテスト。
/// UT はスナップショットの引き当て規則を、ここでは「CSV由来の実データから正しく索引が組め、
/// 単表PK読み+メモリ引き当てが従来の4テーブルJOINと同じ結果を返すこと」を見る。
/// </summary>
[Collection("WebApplication")]
public class OperationNotificationMasterStoreTest(WebApplicationFixture factory)
{
    /// <summary>
    /// DBの track_circuit から「告知器名 → 軌道回路id集合」を組み直す。
    /// スナップショットの組み立てと独立した、このテストの中のオラクル。
    /// </summary>
    private static async Task<Dictionary<string, List<ulong>>> LoadExpectedIndexAsync(
        ApplicationDbContext context, CancellationToken ct)
    {
        var rows = await context.TrackCircuits
            .Where(tc => tc.OperationNotificationDisplayName != null)
            .Select(tc => new { tc.Id, Name = tc.OperationNotificationDisplayName! })
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToList(), StringComparer.Ordinal);
    }

    [Fact(DisplayName = "起動完了時点でスナップショットがDBの内容で載っていること")]
    public async Task Startup_LoadsSnapshotFromDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = factory.Services.GetRequiredService<IOperationNotificationMasterStore>();

        // InitDbHostedService が連動装置マスタのロード後に読むので、この時点で例外にならない
        var master = store.Current;

        var expected = await LoadExpectedIndexAsync(context, ct);
        Assert.NotEmpty(expected);
        foreach (var (displayName, trackCircuitIds) in expected)
        {
            // 告知器の軌道回路をまるごと渡せば、その告知器に着く
            Assert.Equal(displayName, master.TryGetDisplayName(trackCircuitIds));
        }
    }

    [Fact(DisplayName = "告知器の軌道回路に入りきっていない在線ではnullになること")]
    public async Task TryGetDisplayName_PartiallyOccupied_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var master = factory.Services.GetRequiredService<IOperationNotificationMasterStore>().Current;

        var expected = await LoadExpectedIndexAsync(context, ct);
        // 告知器の無い軌道回路を1本混ぜると、どの告知器とも集合一致しなくなる
        var unrelatedId = await context.TrackCircuits
            .Where(tc => tc.OperationNotificationDisplayName == null)
            .Select(tc => tc.Id)
            .FirstAsync(ct);

        foreach (var (_, trackCircuitIds) in expected)
        {
            Assert.Null(master.TryGetDisplayName([..trackCircuitIds, unrelatedId]));
            if (trackCircuitIds.Count > 1)
            {
                // 複数軌道回路にまたがる告知器なら、1本欠けた在線でもnull
                Assert.Null(master.TryGetDisplayName(trackCircuitIds[..^1]));
            }
        }
    }

    [Fact(DisplayName = "告知器の紐づかない軌道回路ではnullになること")]
    public async Task TryGetDisplayName_TrackCircuitWithoutDisplay_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var master = factory.Services.GetRequiredService<IOperationNotificationMasterStore>().Current;

        var ids = await context.TrackCircuits
            .Where(tc => tc.OperationNotificationDisplayName == null)
            .Select(tc => tc.Id)
            .Take(2)
            .ToListAsync(ct);

        Assert.NotEmpty(ids);
        Assert.Null(master.TryGetDisplayName(ids));
    }

    [Fact(DisplayName = "マスタ経由の引き当てが従来の4テーブルJOINと同じ結果を返すこと")]
    public async Task GetOperationNotificationDataByTrackCircuitIds_MatchesJoinQuery()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IOperationNotificationService>();

        var expectedIndex = await LoadExpectedIndexAsync(context, ct);
        // 告知器持ちの軌道回路集合と、告知器の無い軌道回路単体の両方を通す
        var cases = expectedIndex.Values.Take(5).ToList();
        cases.Add(await context.TrackCircuits
            .Where(tc => tc.OperationNotificationDisplayName == null)
            .Select(tc => tc.Id)
            .Take(1)
            .ToListAsync(ct));

        foreach (var trackCircuitIds in cases)
        {
            var actual = await service.GetOperationNotificationDataByTrackCircuitIds(trackCircuitIds);

            // 移行前の経路と同じ形のクエリを、このテストの中でオラクルとして組む
            var displays = await context.TrackCircuits
                .Where(tc => trackCircuitIds.Contains(tc.Id))
                .Include(tc => tc.OperationNotificationDisplay)
                .ThenInclude(d => d!.OperationNotificationState)
                .Include(tc => tc.OperationNotificationDisplay)
                .ThenInclude(d => d!.TrackCircuits)
                .Select(tc => tc.OperationNotificationDisplay)
                .ToListAsync(ct);
            var expectedDisplay = displays.Count == 1 && displays[0] != null
                                  && displays[0]!.TrackCircuits!.Select(tc => tc.Id).ToHashSet()
                                      .SetEquals(trackCircuitIds)
                ? displays[0]
                : null;

            Assert.Equal(expectedDisplay?.Name, actual?.DisplayName);
            Assert.Equal(expectedDisplay?.OperationNotificationState?.Type, actual?.Type);
            Assert.Equal(expectedDisplay?.OperationNotificationState?.Content, actual?.Content);
        }
    }

    [Fact(DisplayName = "司令卓向けの全件取得が告知器の全件と一致すること")]
    public async Task GetOperationNotificationData_CoversAllDisplays()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IOperationNotificationService>();

        var actual = await service.GetOperationNotificationData();

        // state全件 == display全件 (OperationNotificationDisplayDbInitializer が同時に作る)が前提。
        // 崩れると、display本体を読まなくなった今の実装では司令卓から告知器が消える
        var expected = await context.OperationNotificationDisplays
            .Select(d => d.Name)
            .ToListAsync(ct);
        Assert.NotEmpty(expected);
        Assert.Equal(expected.Order(), actual.Select(d => d.DisplayName).Order());
    }
}
