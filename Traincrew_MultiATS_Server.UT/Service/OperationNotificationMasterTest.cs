using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// <see cref="OperationNotificationMaster"/> の引き当て規則のテスト。
/// </summary>
/// <remarks>
/// 判定は「一致」(告知器の軌道回路集合 == 在線集合)であって「包含」ではない。
/// 現行データは告知器1つにつき軌道回路1本しか無いので両者の差は表面化しないが、
/// CSV(OperationNotificationDisplayCsvMap の列2〜4)は1告知器あたり最大3本を許しており、
/// 将来データが増えたときにどちらの意味だったかが分からなくならないよう、
/// ここでテスト用マスタを直接組んで固定する。
/// </remarks>
public class OperationNotificationMasterTest
{
    private static TrackCircuit TrackCircuit(ulong id, string? displayName) => new()
    {
        Id = id,
        Name = $"TC{id}",
        Type = ObjectType.TrackCircuit,
        OperationNotificationDisplayName = displayName
    };

    /// <summary>
    /// 告知器A: 軌道回路 1,2 / 告知器B: 軌道回路 3 / 軌道回路 4 は告知器なし
    /// </summary>
    private static OperationNotificationMaster CreateMaster() => new([
        TrackCircuit(1, "A"),
        TrackCircuit(2, "A"),
        TrackCircuit(3, "B"),
        TrackCircuit(4, null)
    ]);

    [Fact(DisplayName = "告知器の軌道回路集合とぴったり一致すれば告知器名を返すこと")]
    public void TryGetDisplayName_ExactMatch_ReturnsName()
    {
        Assert.Equal("A", CreateMaster().TryGetDisplayName([1, 2]));
    }

    [Fact(DisplayName = "入力順が違っても同じ告知器に着くこと")]
    public void TryGetDisplayName_ExactMatch_IsOrderIndependent()
    {
        // 実装は trackCircuitIds[0] を代表に取るので、先頭がどちらでも結果が変わらないことを見る
        Assert.Equal("A", CreateMaster().TryGetDisplayName([2, 1]));
    }

    [Fact(DisplayName = "軌道回路1本の告知器も引けること")]
    public void TryGetDisplayName_SingleTrackCircuitDisplay_ReturnsName()
    {
        Assert.Equal("B", CreateMaster().TryGetDisplayName([3]));
    }

    [Fact(DisplayName = "告知器の軌道回路に入りきっていなければnullを返すこと")]
    public void TryGetDisplayName_PartiallyOccupied_ReturnsNull()
    {
        // 告知器Aは1,2の2本。1本だけの在線は「まだホームトラックに入りきっていない」
        Assert.Null(CreateMaster().TryGetDisplayName([1]));
    }

    [Fact(DisplayName = "別告知器の軌道回路が混ざればnullを返すこと")]
    public void TryGetDisplayName_MixedDisplays_ReturnsNull()
    {
        // 件数(2)は告知器Aと揃うが中身が違う。Count比較だけで通してしまわないことの確認
        Assert.Null(CreateMaster().TryGetDisplayName([1, 3]));
    }

    [Fact(DisplayName = "告知器の集合を超えてまたがっていればnullを返すこと(包含ではなく一致)")]
    public void TryGetDisplayName_SupersetOfDisplay_ReturnsNull()
    {
        // 告知器Aの1,2に加えて進入側の4にもまたがっている長い列車。
        // 「包含」判定ならAを返すが、採用したのは「一致」なのでnull。
        Assert.Null(CreateMaster().TryGetDisplayName([1, 2, 4]));
    }

    [Fact(DisplayName = "告知器の紐づかない軌道回路だけならnullを返すこと")]
    public void TryGetDisplayName_TrackCircuitWithoutDisplay_ReturnsNull()
    {
        Assert.Null(CreateMaster().TryGetDisplayName([4]));
    }

    [Fact(DisplayName = "先頭が告知器の紐づかない軌道回路でもnullを返すこと")]
    public void TryGetDisplayName_FirstTrackCircuitHasNoDisplay_ReturnsNull()
    {
        // 代表に取る[0]が無所属なら、そもそも集合一致は成立しない
        Assert.Null(CreateMaster().TryGetDisplayName([4, 1, 2]));
    }

    [Fact(DisplayName = "マスタに無い軌道回路idならnullを返すこと")]
    public void TryGetDisplayName_UnknownTrackCircuitId_ReturnsNull()
    {
        Assert.Null(CreateMaster().TryGetDisplayName([999]));
    }

    [Fact(DisplayName = "軌道回路が空ならnullを返すこと")]
    public void TryGetDisplayName_EmptyInput_ReturnsNull()
    {
        Assert.Null(CreateMaster().TryGetDisplayName([]));
    }

    [Fact(DisplayName = "告知器名は序数比較で引かれること")]
    public void TryGetDisplayName_NameLookupIsOrdinal()
    {
        // 索引の比較子を既定(カルチャ依存)に戻すと、大文字小文字や合字違いの告知器名が衝突しうる
        var master = new OperationNotificationMaster([TrackCircuit(1, "a"), TrackCircuit(2, "A")]);

        Assert.Equal("a", master.TryGetDisplayName([1]));
        Assert.Equal("A", master.TryGetDisplayName([2]));
    }
}
