using System.Reflection;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// <see cref="InterlockingObjectMaster"/> の引き当て規則のテスト。
/// 「見つからないものは例外にせず黙って落とす」のが既存SQLと揃った挙動で、
/// TrainService の文字化けフォールバック(件数が合わなければ前回データを使う)がそれに依存している。
/// </summary>
public class InterlockingObjectMasterTest
{
    private static TrackCircuit TrackCircuit(ulong id, string name, string? stationId = null) => new()
    {
        Id = id,
        Name = name,
        Type = ObjectType.TrackCircuit,
        StationId = stationId
    };

    private static Route Route(ulong id, string name, string? stationId = null) => new()
    {
        Id = id,
        Name = name,
        Type = ObjectType.Route,
        TcName = $"tc_{name}",
        RouteType = RouteType.Arriving,
        StationId = stationId
    };

    private static InterlockingObjectMaster CreateMaster(params InterlockingObject[] objects) => new(objects);

    [Fact]
    public void GetByNames_ReturnsInInputOrder()
    {
        var master = CreateMaster(TrackCircuit(1, "TC1"), TrackCircuit(2, "TC2"), TrackCircuit(3, "TC3"));

        var actual = master.GetByNames<TrackCircuit>(["TC3", "TC1"]);

        Assert.Equal(["TC3", "TC1"], actual.Select(tc => tc.Name));
    }

    [Fact]
    public void GetByNames_DuplicatedName_ReturnedOnce()
    {
        var master = CreateMaster(TrackCircuit(1, "TC1"), TrackCircuit(2, "TC2"));

        var actual = master.GetByNames<TrackCircuit>(["TC1", "TC1", "TC2"]);

        Assert.Equal(["TC1", "TC2"], actual.Select(tc => tc.Name));
    }

    [Fact]
    public void GetByNames_UnknownName_SilentlySkipped()
    {
        var master = CreateMaster(TrackCircuit(1, "TC1"));

        var actual = master.GetByNames<TrackCircuit>(["TC1", "存在しない軌道回路"]);

        // 例外にせず件数が減る。呼び出し側はこの件数差で文字化けを検知している
        Assert.Equal(["TC1"], actual.Select(tc => tc.Name));
    }

    [Fact]
    public void GetByNames_TypeMismatch_SilentlySkipped()
    {
        var master = CreateMaster(TrackCircuit(1, "TC1"), Route(2, "R1"));

        var actual = master.GetByNames<TrackCircuit>(["TC1", "R1"]);

        Assert.Equal(["TC1"], actual.Select(tc => tc.Name));
    }

    [Fact]
    public void GetByNames_BaseType_ReturnsEveryType()
    {
        var master = CreateMaster(TrackCircuit(1, "TC1"), Route(2, "R1"));

        var actual = master.GetByNames<InterlockingObject>(["TC1", "R1"]);

        Assert.Equal(["TC1", "R1"], actual.Select(o => o.Name));
    }

    [Fact]
    public void GetByIds_DuplicatesFoldedMissingAndTypeMismatchSkipped()
    {
        var master = CreateMaster(TrackCircuit(1, "TC1"), TrackCircuit(2, "TC2"), Route(3, "R1"));

        var actual = master.GetByIds<TrackCircuit>([2, 2, 1, 3, 999]);

        Assert.Equal([(ulong)2, 1], actual.Select(tc => tc.Id));
    }

    [Fact]
    public void TryGetById_TypeMismatch_ReturnsFalse()
    {
        var master = CreateMaster(Route(1, "R1"));

        Assert.False(master.TryGetById<TrackCircuit>(1, out var mismatched));
        Assert.Null(mismatched);
        Assert.True(master.TryGetById<Route>(1, out var route));
        Assert.Equal("R1", route.Name);
    }

    [Fact]
    public void TryGetByName_UnknownName_ReturnsFalse()
    {
        var master = CreateMaster(TrackCircuit(1, "TC1"));

        Assert.False(master.TryGetByName<TrackCircuit>("TC2", out var missing));
        Assert.Null(missing);
    }

    [Fact]
    public void GetByStationId_GroupsByStationAndReturnsEmptyForUnknown()
    {
        var master = CreateMaster(
            TrackCircuit(1, "TC1", "TH71"),
            TrackCircuit(2, "TC2", "TH71"),
            TrackCircuit(3, "TC3", "TH70"),
            TrackCircuit(4, "TC4"));

        Assert.Equal(["TC1", "TC2"], master.GetByStationId("TH71").Select(o => o.Name));
        Assert.Empty(master.GetByStationId("存在しない停車場"));
    }

    [Fact]
    public void Constructor_DuplicatedName_Throws()
    {
        // interlocking_object.name は UNIQUE。重複していたら起動時に落ちるのが正しい
        Assert.Throws<ArgumentException>(() => CreateMaster(TrackCircuit(1, "TC1"), TrackCircuit(2, "TC1")));
    }

    [Fact]
    public void CloneWithState_CopiesEveryProperty()
    {
        var original = new TrackCircuit
        {
            Id = 42,
            Name = "TC42",
            Type = ObjectType.TrackCircuit,
            Description = "説明",
            StationId = "TH71",
            ProtectionZone = 7,
            OperationNotificationDisplayName = "告知器",
            OperationNotificationDisplay = new()
            {
                Name = "告知器", StationId = "TH71", IsUp = true, IsDown = false
            },
            StationIdForDelay = "TH70",
            StationForDelay = new()
            {
                Id = "TH70", Name = "駅", IsStation = true, IsPassengerStation = true
            },
            TrackCircuitState = new() { Id = 42, TrainNumber = "1234" }
        };

        var state = original.TrackCircuitState;

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Same(state, clone.TrackCircuitState);
        // 将来カラムが増えたとき、複製を手書きに変えてコピー漏れしたらここで落ちる
        foreach (var property in typeof(TrackCircuit).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.Equal(property.GetValue(original), property.GetValue(clone));
        }
    }

    [Fact]
    public void CloneWithState_DoesNotTouchSnapshotInstance()
    {
        var master = TrackCircuit(1, "TC1");

        var clone = master.CloneWithState(new() { Id = 1, TrainNumber = "1234", IsShortCircuit = true });

        // 共有インスタンスであるマスタ側が汚染されないこと(汚染するとリクエスト間で競合する)
        Assert.Null(master.TrackCircuitState);
        Assert.True(clone.TrackCircuitState.IsShortCircuit);
    }
}
