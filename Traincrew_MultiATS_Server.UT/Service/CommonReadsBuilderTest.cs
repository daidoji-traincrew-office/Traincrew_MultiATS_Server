using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Services;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// <see cref="CommonReadsBuilder"/> の組み立て規則のテスト。
/// マスタ(<see cref="BroadcastMasterData"/>)と、ローダーが返した状態込みの連動オブジェクトから、
/// 配信用の共通データを組み立てる。実DBとの一致は IT の差分テストで見る。
/// </summary>
public class CommonReadsBuilderTest
{
    private const ulong ActiveRouteId = 1;
    private const ulong InactiveRouteId = 2;
    private const ulong TrackCircuitId = 10;
    private const ulong DirectionRouteId = 20;

    private readonly Mock<IBroadcastMasterStore> _masterStore = new();
    private readonly Mock<IInterlockingObjectStateLoader> _loader = new();
    private readonly Mock<IServerRepository> _serverRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<ITtcWindowRepository> _ttcWindowRepository = new();
    private readonly Mock<IDestinationButtonRepository> _destinationButtonRepository = new();
    private readonly Mock<ITrainService> _trainService = new();

    private readonly Route _activeRoute = new()
    {
        Id = ActiveRouteId, Name = "R1", Type = ObjectType.Route, TcName = "tc1",
        RouteState = new() { Id = ActiveRouteId, IsRouteSecured = RaiseDrop.Raise }
    };

    private readonly Route _inactiveRoute = new()
    {
        Id = InactiveRouteId, Name = "R2", Type = ObjectType.Route, TcName = "tc2",
        RouteState = new() { Id = InactiveRouteId, IsRouteSecured = RaiseDrop.Drop }
    };

    private readonly TrackCircuit _trackCircuit = new()
    {
        Id = TrackCircuitId, Name = "TC", Type = ObjectType.TrackCircuit,
        TrackCircuitState = new() { Id = TrackCircuitId, TrainNumber = "" }
    };

    private readonly DirectionRoute _directionRoute = new()
    {
        Id = DirectionRouteId, Name = "DR", Type = ObjectType.DirectionRoute, LeverId = 30,
        DirectionRouteState = new() { Id = DirectionRouteId, isLr = LR.Right }
    };

    private static SignalType Type() => new()
    {
        Name = "3灯", RIndication = SignalIndication.R, YYIndication = SignalIndication.YY,
        YIndication = SignalIndication.Y, YGIndication = SignalIndication.YG, GIndication = SignalIndication.G
    };

    private CommonReadsBuilder CreateBuilder(BroadcastMasterData master)
    {
        _masterStore.SetupGet(s => s.Current).Returns(master);
        _loader.Setup(l => l.GetAllWithStateForBroadcast()).ReturnsAsync(new InterlockingObjectsWithState(
            [_activeRoute, _inactiveRoute], [], [_trackCircuit], [], [_directionRoute], [], []));
        _serverRepository.Setup(r => r.GetServerStateForBroadcastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerState { TimeOffset = 123, SelectedDiagramId = 7 });
        _stationRepository.Setup(r => r.GetAllTimerStatesForBroadcast(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new() { StationId = "S1", Seconds = 5, IsTimerConditionMet = true },
                new() { StationId = "S9", Seconds = 5 }
            ]);
        _ttcWindowRepository.Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Name = "W1", TrainNumber = "回1" }]);
        _destinationButtonRepository.Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Name = "B1", IsRaised = RaiseDrop.Raise }]);
        _trainService.Setup(s => s.GetAllTrainState()).ReturnsAsync([]);

        return new(
            _masterStore.Object, _loader.Object, _serverRepository.Object, _stationRepository.Object,
            _ttcWindowRepository.Object, _destinationButtonRepository.Object, _trainService.Object);
    }

    private static BroadcastMasterData Master(
        IReadOnlyList<BroadcastSignalMaster>? signals = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? nextSignals = null,
        IReadOnlyDictionary<string, IReadOnlyList<ulong>>? routeIds = null) => new(
        ["S1"],
        signals ?? [],
        nextSignals ?? new Dictionary<string, IReadOnlyList<string>>(),
        routeIds ?? new Dictionary<string, IReadOnlyList<ulong>>(),
        [
            new("W1", "S1", TtcWindowType.Up, []),
            new("W2", "S2", TtcWindowType.Up, ["S1"]),
            new("W3", "S2", TtcWindowType.Up, ["S3"])
        ],
        [new("B1", "S1")]);

    [Fact(DisplayName = "列番窓は所属駅または表示駅が駅一覧に含まれるものだけを、状態込みで返す")]
    public async Task BuildAsync_TtcWindows_FilteredByStationOrDisplayStation()
    {
        var reads = await CreateBuilder(Master()).BuildAsync();

        Assert.Equal(["W1", "W2"], reads.TtcWindows.Select(w => w.Name).Order());
        Assert.Equal("回1", reads.TtcWindows.Single(w => w.Name == "W1").TtcWindowState?.TrainNumber);
        // 状態行が無い窓はnullのまま(ToRetsubanDataが空文字にする)
        Assert.Null(reads.TtcWindows.Single(w => w.Name == "W2").TtcWindowState);
    }

    [Fact(DisplayName = "駅の時素状態は駅一覧に含まれる駅だけに絞る")]
    public async Task BuildAsync_StationTimerStates_FilteredByStation()
    {
        var reads = await CreateBuilder(Master()).BuildAsync();

        Assert.Equal(["S1"], reads.StationIds);
        Assert.Equal(["S1"], reads.StationTimerStates.Select(s => s.StationId));
    }

    [Fact(DisplayName = "有効進路は進路確保が扛上している進路だけ。server_stateは1回の読み取りの値を共有する")]
    public async Task BuildAsync_ActiveRoutes_AndServerStateSnapshot()
    {
        var reads = await CreateBuilder(Master()).BuildAsync();

        Assert.Equal(["tc1"], reads.ActiveRoutes.Select(r => r.TcName));
        Assert.Equal(123, reads.TimeOffset);
        Assert.Equal(7UL, reads.ServerState.SelectedDiagramId);
        _serverRepository.Verify(r => r.GetServerStateForBroadcastAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(["B1"], reads.DestinationButtons.Select(b => b.Name));
    }

    [Fact(DisplayName = "マスタストアのCurrentはティックの開始時に1回だけ読み、ローダーも1回だけ呼ぶ")]
    public async Task BuildAsync_ReadsMasterAndLoaderOnce()
    {
        await CreateBuilder(Master()).BuildAsync();

        _masterStore.VerifyGet(s => s.Current, Times.Once);
        _loader.Verify(l => l.GetAllWithStateForBroadcast(), Times.Once);
    }

    [Fact(DisplayName = "信号現示の入力は、ローダーが返した同じ軌道回路・方向進路・進路のインスタンスを参照する")]
    public async Task BuildAsync_SignalInput_SharesLoadedInstances()
    {
        var master = Master(
            [new("SIG", "3灯", "S1", Type(), TrackCircuitId, DirectionRouteId, null, LR.Right)],
            new Dictionary<string, IReadOnlyList<string>> { ["SIG"] = ["NEXT"] },
            new Dictionary<string, IReadOnlyList<ulong>> { ["SIG"] = [ActiveRouteId, 999] });

        var reads = await CreateBuilder(master).BuildAsync();

        var signal = reads.SignalInput.Signals["SIG"];
        Assert.Same(_trackCircuit, signal.TrackCircuit);
        Assert.Same(_directionRoute, signal.DirectionRouteLeft);
        Assert.Null(signal.DirectionRouteRight);
        // マスタに無い進路IDは黙って落とす(旧INNER JOINと同じ)
        Assert.Same(_activeRoute, Assert.Single(reads.SignalInput.Routes["SIG"]));
        Assert.Equal(["NEXT"], reads.SignalInput.NextSignalNames["SIG"]);
    }

    [Fact(DisplayName = "配信側が加工しても、マスタ側のリストは変化しない(コピーを渡す)")]
    public async Task BuildAsync_MutatingResult_DoesNotAffectMaster()
    {
        var nextSignals = new Dictionary<string, IReadOnlyList<string>> { ["SIG"] = ["NEXT"] };
        var master = Master(
            [new("SIG", "3灯", "S1", Type(), null, null, null, null)],
            nextSignals);

        var reads = await CreateBuilder(master).BuildAsync();
        reads.StationIds.Add("X");
        reads.SignalInput.NextSignalNames["SIG"].Add("MUTATED");

        Assert.Equal(["S1"], master.StationIds);
        Assert.Equal(["NEXT"], master.NextSignalNamesBySignalName["SIG"]);
    }
}
