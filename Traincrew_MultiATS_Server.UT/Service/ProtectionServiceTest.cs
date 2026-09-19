using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Protection;
using Traincrew_MultiATS_Server.Services;
using Traincrew_MultiATS_Server.UT.TestHelpers;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// <see cref="ProtectionService.EvaluateAndUpdateBougo"/> のテスト。
/// </summary>
/// <remarks>
/// 防護無線は「キャッシュが古くて発報できない/受報できない」のが最も危険なため、
/// 判定は必ずそのtickで読んだ protection_zone_state に対して行う設計になっている。
/// ここで固定したいのは主に次の3つ:
/// 1. 受報判定が在線軌道回路の集合全体から防護範囲を取っていること(境界にまたがった場合)
/// 2. 受報状態が1tickで切り替わること(進入・離脱の両方)
/// 3. 空振りDELETEの抑制が「解除が1回も打たれない」バグに化けていないこと
/// </remarks>
public class ProtectionServiceTest : ServiceTestBase
{
    private const string TrainNumber = "1234M";
    private const string OtherTrainNumber = "5678M";

    private readonly Mock<IProtectionRepository> _protectionRepositoryMock = new();

    /// <summary>
    /// DB上の protection_zone_state を模した状態。
    /// EnableProtection / DisableProtection の呼び出しで実際に書き換わるので、
    /// tickを連続で回したときの挙動をそのまま再現できる。
    /// </summary>
    private readonly List<ProtectionZoneState> _rows = [];

    /// <summary>EnableProtection に渡されたゾーンリストの履歴</summary>
    private readonly List<List<int>> _enableCalls = [];

    private ulong _nextId = 1;

    protected override void ConfigureTestServices(ServiceCollection services)
    {
        services.AddAllMocks();
        services.ReplaceMock(_protectionRepositoryMock);
        services.UseRealService<IProtectionService, ProtectionService>();
    }

    public ProtectionServiceTest()
    {
        _protectionRepositoryMock
            .Setup(r => r.GetAll())
            // 毎回コピーを返す(サービス側が受け取ったリストを握り続けても後続tickに影響しないように)
            .ReturnsAsync(() => _rows.Select(CloneRow).ToList());

        _protectionRepositoryMock
            .Setup(r => r.Enable(It.IsAny<string>(), It.IsAny<List<int>>()))
            .Callback<string, List<int>>((trainNumber, zones) =>
            {
                _enableCalls.Add([..zones]);
                // 実装(ProtectionRepository.EnableProtection)と同じ差分適用をする
                _rows.RemoveAll(row => row.TrainNumber == trainNumber && !zones.Contains(row.ProtectionZone));
                foreach (var zone in zones.Distinct())
                {
                    if (_rows.Any(row => row.TrainNumber == trainNumber && row.ProtectionZone == zone))
                    {
                        continue;
                    }

                    _rows.Add(new() { id = _nextId++, TrainNumber = trainNumber, ProtectionZone = zone });
                }
            })
            .Returns(Task.CompletedTask);

        _protectionRepositoryMock
            .Setup(r => r.Disable(It.IsAny<string>()))
            .Callback<string>(trainNumber => _rows.RemoveAll(row => row.TrainNumber == trainNumber))
            .Returns(Task.CompletedTask);
    }

    private static ProtectionZoneState CloneRow(ProtectionZoneState row) =>
        new() { id = row.id, TrainNumber = row.TrainNumber, ProtectionZone = row.ProtectionZone };

    private void GivenEmitting(string trainNumber, params int[] zones)
    {
        foreach (var zone in zones)
        {
            _rows.Add(new() { id = _nextId++, TrainNumber = trainNumber, ProtectionZone = zone });
        }
    }

    private static List<TrackCircuit> OnTrack(params int[] protectionZones) =>
        protectionZones
            .Select((zone, index) => new TrackCircuit
            {
                Id = (ulong)(index + 1),
                Name = $"TC{index + 1}",
                Type = ObjectType.TrackCircuit,
                ProtectionZone = zone
            })
            .ToList();

    private Task<bool> Tick(List<TrackCircuit> onTrack, bool clientBougoState) =>
        GetService<IProtectionService>()
            .EvaluateAndUpdateBougo(TrainNumber, onTrack, clientBougoState);

    // ---------------------------------------------------------------- 書き込み判定

    [Fact(DisplayName = "自分の行が無く未発報なら、空振りのDELETEを打たないこと")]
    public async Task NotEmitting_WithoutOwnRow_DoesNotDelete()
    {
        await Tick(OnTrack(10), false);

        _protectionRepositoryMock.Verify(r => r.Disable(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "自分の行があって未発報なら、解除のDELETEを打つこと")]
    public async Task NotEmitting_WithOwnRow_Deletes()
    {
        GivenEmitting(TrainNumber, 10);

        await Tick(OnTrack(10), false);

        _protectionRepositoryMock.Verify(r => r.Disable(TrainNumber), Times.Once);
    }

    [Fact(DisplayName = "他列車の行だけがある場合は、自分の解除を打たないこと")]
    public async Task NotEmitting_WithOnlyOtherTrainRow_DoesNotDelete()
    {
        GivenEmitting(OtherTrainNumber, 10);

        await Tick(OnTrack(10), false);

        _protectionRepositoryMock.Verify(r => r.Disable(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "発報中は、既に同じゾーン集合が入っていても毎回書き込むこと")]
    public async Task Emitting_WritesEvenWhenZonesAlreadyMatch()
    {
        // 読みと書きの間に司令卓が行を消すと発報漏れになるため、発報側の省略はしない
        GivenEmitting(TrainNumber, 10);

        await Tick(OnTrack(10), true);

        _protectionRepositoryMock.Verify(
            r => r.Enable(TrainNumber, It.IsAny<List<int>>()), Times.Once);
    }

    [Fact(DisplayName = "在線が空でも例外にならず、受報なしを返すこと")]
    public async Task EmptyTrackCircuits_ReturnsFalseWithoutThrowing()
    {
        // 統合前は Min()/Max() が空リストで InvalidOperationException になっていた
        GivenEmitting(OtherTrainNumber, 10);

        Assert.False(await Tick(OnTrack(), false));
    }

    // ---------------------------------------------------------------- 受報判定(静的)

    [Theory(DisplayName = "在線1本のとき、防護範囲は在線ゾーンの±1であること")]
    [InlineData(8, false)]
    [InlineData(9, true)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    [InlineData(12, false)]
    public async Task SingleTrackCircuit_RangeIsZonePlusMinusOne(int emittingZone, bool expected)
    {
        GivenEmitting(OtherTrainNumber, emittingZone);

        Assert.Equal(expected, await Tick(OnTrack(10), false));
    }

    // ------------------------------------------- 受報判定(発報中に受報側が移動するケース)

    [Theory(DisplayName = "ゾーン境界にまたがって停止したとき、防護範囲が在線集合全体から取られること")]
    [InlineData(8, false)]  // 在線{10,11} の min-2
    [InlineData(9, true)]   // min-1
    [InlineData(12, true)]  // max+1: 片方の軌道回路だけ見ていたら取りこぼす位置
    [InlineData(13, false)] // max+2
    public async Task StraddlingZoneBoundary_RangeSpansBothZones(int emittingZone, bool expected)
    {
        // EBを扱ってゾーン境界上に停止し、在線が2ゾーンにまたがっている状況
        GivenEmitting(OtherTrainNumber, emittingZone);

        Assert.Equal(expected, await Tick(OnTrack(10, 11), false));
    }

    [Fact(DisplayName = "境界にまたがった在線の順序が違っても同じ判定になること")]
    public async Task StraddlingZoneBoundary_IsOrderIndependent()
    {
        GivenEmitting(OtherTrainNumber, 12);

        Assert.True(await Tick(OnTrack(10, 11), false));
        Assert.True(await Tick(OnTrack(11, 10), false));
    }

    [Fact(DisplayName = "防護範囲に進入した次のtickで受報すること")]
    public async Task MovingIntoProtectionZone_StartsReceivingOnNextTick()
    {
        // 発報状態は変えず、受報側の在線だけを動かす。
        // キャッシュを持たないので1tickで切り替わる。ここが古い値を返すと受報漏れになる。
        GivenEmitting(OtherTrainNumber, 20);

        Assert.False(await Tick(OnTrack(10), false));
        Assert.True(await Tick(OnTrack(19), false));
    }

    [Fact(DisplayName = "防護範囲から抜けた次のtickで受報しなくなること")]
    public async Task MovingOutOfProtectionZone_StopsReceivingOnNextTick()
    {
        GivenEmitting(OtherTrainNumber, 20);

        Assert.True(await Tick(OnTrack(21), false));
        Assert.False(await Tick(OnTrack(30), false));
    }

    [Fact(DisplayName = "境界にまたがって進入した瞬間に受報すること")]
    public async Task StraddlingIntoProtectionZone_StartsReceiving()
    {
        GivenEmitting(OtherTrainNumber, 20);

        // 在線{17}のとき範囲は[16,18]で範囲外
        Assert.False(await Tick(OnTrack(17), false));
        // 境界にまたがって在線{17,18}になると範囲は[16,19]に広がり、発報ゾーン20は…まだ範囲外
        Assert.False(await Tick(OnTrack(17, 18), false));
        // さらに進んで在線{18,19}になると範囲は[17,20]で受報する
        Assert.True(await Tick(OnTrack(18, 19), false));
    }

    // ------------------------------------ 発報側が発報しながら移動するケース(EB→防護発報→停止)

    [Fact(DisplayName = "発報中にゾーンへ進入したら、新しいゾーンが防護範囲に加わること")]
    public async Task EmittingWhileMovingIntoNewZone_AddsZone()
    {
        // 1tick目: 在線{10}
        await Tick(OnTrack(10), true);
        // 2tick目: 境界にまたがって在線{10,11}
        await Tick(OnTrack(10, 11), true);

        Assert.Equal([10], _enableCalls[0]);
        Assert.Equal([10, 11], _enableCalls[1]);
        Assert.Equal([10, 11], _rows.Select(row => row.ProtectionZone).Order().ToList());
    }

    [Fact(DisplayName = "発報中にゾーンから抜けたら、古いゾーンが防護範囲から落ちること")]
    public async Task EmittingWhileMovingOutOfZone_RemovesZone()
    {
        // 境界にまたがって発報 → 抜け切って停止
        await Tick(OnTrack(10, 11), true);
        await Tick(OnTrack(11), true);

        Assert.Equal([10, 11], _enableCalls[0]);
        Assert.Equal([11], _enableCalls[1]);
        // 古いゾーン10の行が残っていないこと(残ると防護範囲が広がったままになる)
        Assert.Equal([11], _rows.Select(row => row.ProtectionZone).Order().ToList());
    }

    [Fact(DisplayName = "停止後も発報し続けている間、毎tick書き込むこと")]
    public async Task EmittingWhileStopped_WritesEveryTick()
    {
        var onTrack = OnTrack(10);
        await Tick(onTrack, true);
        await Tick(onTrack, true);
        await Tick(onTrack, true);

        // ゾーン集合が一致していても省略しない = 発報漏れが構造的に起きない
        _protectionRepositoryMock.Verify(
            r => r.Enable(TrainNumber, It.IsAny<List<int>>()), Times.Exactly(3));
    }

    [Fact(DisplayName = "発報を解除したとき、解除のDELETEが確実に1回打たれること")]
    public async Task ReleasingProtection_DeletesExactlyOnce()
    {
        // 空振り抑制が「解除が1回も打たれない」バグに化けていないことの固定。
        // これが §1 で最も壊してはいけない不変条件。
        await Tick(OnTrack(10), true);

        await Tick(OnTrack(10), false);
        await Tick(OnTrack(10), false);

        _protectionRepositoryMock.Verify(r => r.Disable(TrainNumber), Times.Once);
        Assert.Empty(_rows);
    }

    [Fact(DisplayName = "在線が空のまま発報すると、実質解除になること(統合前と同じ挙動)")]
    public async Task EmittingWithEmptyTrackCircuits_ClearsOwnRows()
    {
        await Tick(OnTrack(10), true);

        await Tick(OnTrack(), true);

        Assert.Equal([], _enableCalls[1]);
        Assert.Empty(_rows);
    }

    [Fact(DisplayName = "自分の発報が自分の受報に反映されるのは次のtickであること(統合前と同じ挙動)")]
    public async Task OwnEmission_IsReceivedFromNextTick()
    {
        // 受報判定は書き込み前のスナップショットで行うため1tick遅れる
        Assert.False(await Tick(OnTrack(10), true));
        Assert.True(await Tick(OnTrack(10), true));
    }
}
