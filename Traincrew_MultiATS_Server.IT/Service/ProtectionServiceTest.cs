using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.IT.Service;

/// <summary>
/// 発報しながら移動したときに protection_zone_state の行が在線に追随することのテスト。
/// </summary>
/// <remarks>
/// UT はリポジトリをモックしているため、ゾーンの追加・削除の差分適用そのものは検証できない。
/// ここでは実DBに対して、発報中にゾーンを移動したときに
/// 新しいゾーンが追加され、古いゾーンが残らないことを見る。
/// </remarks>
[Collection("WebApplication")]
public class ProtectionServiceTest(WebApplicationFixture factory) : IAsyncLifetime
{
    /// <summary>他のテストの列車番号と衝突しないようにこのテスト専用の番号を使う</summary>
    private const string TrainNumber = "9999ProtectionIT";

    public ValueTask InitializeAsync() => CleanupAsync();

    public ValueTask DisposeAsync() => CleanupAsync();

    /// <summary>このテストが作った行を消す。残骸が残ると後続のテストの受報判定に影響する。</summary>
    private async ValueTask CleanupAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.protectionZoneStates
            .Where(state => state.TrainNumber == TrainNumber)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<int>> ZonesOfTestTrainAsync(CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.protectionZoneStates
            .AsNoTracking()
            .Where(state => state.TrainNumber == TrainNumber)
            .Select(state => state.ProtectionZone)
            .OrderBy(zone => zone)
            .ToListAsync(ct);
    }

    /// <summary>実データから防護ゾーンが異なる軌道回路を2本取る</summary>
    private async Task<(TrackCircuit First, TrackCircuit Second)> TwoTrackCircuitsInDifferentZonesAsync(
        CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var trackCircuits = await context.TrackCircuits
            .AsNoTracking()
            .OrderBy(tc => tc.ProtectionZone)
            .ThenBy(tc => tc.Id)
            .ToListAsync(ct);

        var first = trackCircuits[0];
        var second = trackCircuits.First(tc => tc.ProtectionZone != first.ProtectionZone);
        return (first, second);
    }

    private async Task TickAsync(List<TrackCircuit> onTrack, bool clientBougoState)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var protectionService = scope.ServiceProvider.GetRequiredService<IProtectionService>();
        await protectionService.EvaluateAndUpdateBougo(TrainNumber, onTrack, clientBougoState);
    }

    [Fact(DisplayName = "発報しながらゾーンを移動すると、防護範囲の行が在線に追随すること")]
    public async Task EmittingWhileMoving_RowsFollowOccupiedZones()
    {
        var ct = TestContext.Current.CancellationToken;
        var (first, second) = await TwoTrackCircuitsInDifferentZonesAsync(ct);

        // 1. 在線{first}で発報
        await TickAsync([first], true);
        Assert.Equal([first.ProtectionZone], await ZonesOfTestTrainAsync(ct));

        // 2. 境界にまたがって在線{first, second}: 新しいゾーンが追加される
        await TickAsync([first, second], true);
        Assert.Equal(
            new[] { first.ProtectionZone, second.ProtectionZone }.Order().ToList(),
            await ZonesOfTestTrainAsync(ct));

        // 3. 抜け切って在線{second}: 古いゾーンが残らない
        await TickAsync([second], true);
        Assert.Equal([second.ProtectionZone], await ZonesOfTestTrainAsync(ct));

        // 4. 発報解除で全部消える
        await TickAsync([second], false);
        Assert.Empty(await ZonesOfTestTrainAsync(ct));
    }

    [Fact(DisplayName = "同一ゾーンの軌道回路が複数在線していても一意制約違反にならないこと")]
    public async Task EmittingWithDuplicatedZones_DoesNotViolateUniqueConstraint()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var trackCircuit = await context.TrackCircuits.AsNoTracking().FirstAsync(ct);

        // 渡すゾーンリストには重複が入る(EnableProtection 側の Except が重複を落とす)
        await TickAsync([trackCircuit, trackCircuit], true);

        Assert.Equal([trackCircuit.ProtectionZone], await ZonesOfTestTrainAsync(ct));
    }
}
