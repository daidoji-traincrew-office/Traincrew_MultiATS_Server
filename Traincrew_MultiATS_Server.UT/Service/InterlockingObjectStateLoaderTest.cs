using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Services;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// InterlockingObjectStateLoader のユニットテスト。
/// マスタは本物の InterlockingObjectMasterStore を使う(IInterlockingObjectRepository.GetAllAsync のみモック)。
/// これは公開側 InterlockingObjectMasterStoreTest の CreateStore と同じ組み方
/// (InterlockingObjectMaster は internal コンストラクタのため、直接 new できない)。
/// 各サブタイプの GetStateByIds だけをモックして、呼び出し回数と合成結果を検証する。
/// </summary>
public class InterlockingObjectStateLoaderTest
{
    private const ulong RouteId = 1;
    private const ulong SwitchingMachineId = 2;
    private const ulong TrackCircuitId = 3;
    private const ulong LeverId = 4;
    private const ulong DirectionRouteId = 5;
    private const ulong DirectionSelfControlLeverId = 6;
    private const ulong RouteCentralControlLeverId = 7;
    private const ulong UnknownId = 999;

    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<ISwitchingMachineRepository> _switchingMachineRepositoryMock = new();
    private readonly Mock<ITrackCircuitRepository> _trackCircuitRepositoryMock = new();
    private readonly Mock<ILeverRepository> _leverRepositoryMock = new();
    private readonly Mock<IDirectionRouteRepository> _directionRouteRepositoryMock = new();
    private readonly Mock<IDirectionSelfControlLeverRepository> _directionSelfControlLeverRepositoryMock = new();
    private readonly Mock<IRouteCentralControlLeverRepository> _routeCentralControlLeverRepositoryMock = new();

    private static InterlockingObject CreateMaster(ulong id, ObjectType type) => type switch
    {
        ObjectType.Route => new Route { Id = id, Name = $"R{id}", Type = type, TcName = "" },
        ObjectType.SwitchingMachine => new SwitchingMachine { Id = id, Name = $"SM{id}", Type = type, TcName = "" },
        ObjectType.TrackCircuit => new TrackCircuit { Id = id, Name = $"TC{id}", Type = type },
        ObjectType.Lever => new Lever { Id = id, Name = $"L{id}", Type = type },
        ObjectType.DirectionRoute => new DirectionRoute { Id = id, Name = $"DR{id}", Type = type, LeverId = LeverId },
        ObjectType.DirectionSelfControlLever => new DirectionSelfControlLever
            { Id = id, Name = $"DSCL{id}", Type = type },
        ObjectType.RouteCentralControlLever => new RouteCentralControlLever
            { Id = id, Name = $"RCCL{id}", Type = type },
        _ => throw new InvalidOperationException($"未知のObjectType: {type}")
    };

    private static List<InterlockingObject> AllTypeMasters() =>
    [
        CreateMaster(RouteId, ObjectType.Route),
        CreateMaster(SwitchingMachineId, ObjectType.SwitchingMachine),
        CreateMaster(TrackCircuitId, ObjectType.TrackCircuit),
        CreateMaster(LeverId, ObjectType.Lever),
        CreateMaster(DirectionRouteId, ObjectType.DirectionRoute),
        CreateMaster(DirectionSelfControlLeverId, ObjectType.DirectionSelfControlLever),
        CreateMaster(RouteCentralControlLeverId, ObjectType.RouteCentralControlLever)
    ];

    private async Task<InterlockingObjectStateLoader> CreateLoaderAsync(List<InterlockingObject> masters)
    {
        var interlockingObjectRepositoryMock = new Mock<IInterlockingObjectRepository>();
        interlockingObjectRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(masters);
        // Singleton から Scoped なリポジトリを取る本番と同じ経路を通す(公開側テストと同じ組み方)
        var provider = new ServiceCollection()
            .AddScoped(_ => interlockingObjectRepositoryMock.Object)
            .BuildServiceProvider();
        var masterStore = new InterlockingObjectMasterStore(
            provider.GetRequiredService<IServiceScopeFactory>(), new MutexRepository());
        await masterStore.ReloadAsync();

        return new(
            masterStore,
            _routeRepositoryMock.Object,
            _switchingMachineRepositoryMock.Object,
            _trackCircuitRepositoryMock.Object,
            _leverRepositoryMock.Object,
            _directionRouteRepositoryMock.Object,
            _directionSelfControlLeverRepositoryMock.Object,
            _routeCentralControlLeverRepositoryMock.Object);
    }

    [Fact(DisplayName = "含まれる型のGetStateByIdsだけが各1回呼ばれ、含まれない型は呼ばれない")]
    public async Task GetByIdsWithState_OnlyQueriesStateForIncludedSubtypes()
    {
        _routeRepositoryMock
            .Setup(r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Id = RouteId }]);
        _leverRepositoryMock
            .Setup(r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Id = LeverId }]);

        var loader = await CreateLoaderAsync(AllTypeMasters());

        // 対象はRoute/Leverのみ(他5型は今回の対象に含まれない)
        var result = await loader.GetByIdsWithState([RouteId, LeverId]);

        Assert.Equal(2, result.Count);
        _routeRepositoryMock.Verify(
            r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()), Times.Once);
        _leverRepositoryMock.Verify(
            r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()), Times.Once);
        _switchingMachineRepositoryMock.Verify(
            r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()), Times.Never);
        _trackCircuitRepositoryMock.Verify(
            r => r.GetStateByIds(It.IsAny<List<ulong>>(), It.IsAny<CancellationToken>()), Times.Never);
        _directionRouteRepositoryMock.Verify(
            r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()), Times.Never);
        _directionSelfControlLeverRepositoryMock.Verify(
            r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()), Times.Never);
        _routeCentralControlLeverRepositoryMock.Verify(
            r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "入力IDの重複は畳まれ、結果は1件・同じStateインスタンスを参照する")]
    public async Task GetByIdsWithState_DuplicateIds_CollapsedToSingleEntry()
    {
        var routeState = new RouteState { Id = RouteId };
        _routeRepositoryMock
            .Setup(r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([routeState]);

        var loader = await CreateLoaderAsync(AllTypeMasters());

        var result = await loader.GetByIdsWithState([RouteId, RouteId, RouteId]);

        Assert.Single(result);
        var route = Assert.IsType<Route>(result[RouteId]);
        Assert.Same(routeState, route.RouteState);
        // 重複IDを渡しても、GetStateByIdsへ渡すID集合は1件に畳まれ、1回だけ呼ばれる
        _routeRepositoryMock.Verify(r => r.GetStateByIds(
            It.Is<IEnumerable<ulong>>(ids => ids.SequenceEqual(new[] { RouteId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "State行が無い場合はnullを載せる(非null引数の型を含む)")]
    public async Task GetByIdsWithState_MissingState_LoadsNull()
    {
        // Route(State?の型)・SwitchingMachine(CloneWithStateが非null引数の型)ともに、
        // GetStateByIdsが空リストを返す(=対応するState行が無い)ケースを再現する
        _routeRepositoryMock
            .Setup(r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _switchingMachineRepositoryMock
            .Setup(r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var loader = await CreateLoaderAsync(AllTypeMasters());

        var result = await loader.GetByIdsWithState([RouteId, SwitchingMachineId]);

        var route = Assert.IsType<Route>(result[RouteId]);
        Assert.Null(route.RouteState);
        var switchingMachine = Assert.IsType<SwitchingMachine>(result[SwitchingMachineId]);
        Assert.Null(switchingMachine.SwitchingMachineState);
    }

    [Fact(DisplayName = "返り値はマスタの複製であり、マスタ本体のStateプロパティは変化しない")]
    public async Task GetByIdsWithState_ReturnsCloneDistinctFromMaster()
    {
        var routeState = new RouteState { Id = RouteId };
        _routeRepositoryMock
            .Setup(r => r.GetStateByIds(It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([routeState]);

        var masters = AllTypeMasters();
        var loader = await CreateLoaderAsync(masters);

        var result = await loader.GetByIdsWithState([RouteId]);

        var clonedRoute = Assert.IsType<Route>(result[RouteId]);
        var masterRoute = Assert.IsType<Route>(masters.Single(o => o.Id == RouteId));
        Assert.NotSame(masterRoute, clonedRoute);
        Assert.Null(masterRoute.RouteState);
        Assert.Same(routeState, clonedRoute.RouteState);
    }

    [Fact(DisplayName = "マスタに存在しないIDは結果に含まれない")]
    public async Task GetByIdsWithState_UnknownId_NotIncludedInResult()
    {
        var loader = await CreateLoaderAsync(AllTypeMasters());

        var result = await loader.GetByIdsWithState([UnknownId]);

        Assert.Empty(result);
    }

    [Fact(DisplayName = "配信用の全件取得: 各型の配信専用GetAllStatesForBroadcastが各1回だけ呼ばれ、状態込みの複製がID昇順で返る")]
    public async Task GetAllWithStateForBroadcast_QueriesEachSubtypeOnce_AndReturnsClonesInIdOrder()
    {
        var routeState = new RouteState { Id = RouteId };
        var trackCircuitState = new TrackCircuitState { Id = TrackCircuitId, TrainNumber = "" };
        _routeRepositoryMock
            .Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>())).ReturnsAsync([routeState]);
        _switchingMachineRepositoryMock
            .Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _trackCircuitRepositoryMock
            .Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>())).ReturnsAsync([trackCircuitState]);
        _leverRepositoryMock
            .Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _directionRouteRepositoryMock
            .Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _directionSelfControlLeverRepositoryMock
            .Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _routeCentralControlLeverRepositoryMock
            .Setup(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // マスタ側の並びは逆順で渡す(ID昇順に整列されること)
        var masters = AllTypeMasters();
        masters.Reverse();
        var loader = await CreateLoaderAsync(masters);

        var result = await loader.GetAllWithStateForBroadcast();

        var route = Assert.Single(result.Routes);
        Assert.Same(routeState, route.RouteState);
        var trackCircuit = Assert.Single(result.TrackCircuits);
        Assert.Same(trackCircuitState, trackCircuit.TrackCircuitState);
        // 同じIDのStateは1インスタンス(IDで引いた複製が同じ)
        Assert.Same(trackCircuit, result.TrackCircuitsById[TrackCircuitId]);
        Assert.Same(route, result.RoutesById[RouteId]);
        // State行が無い型はnullを載せる(旧LEFT JOINと同じ)
        Assert.Null(Assert.Single(result.SwitchingMachines).SwitchingMachineState);
        // マスタ本体のStateは変化しない
        Assert.Null(Assert.IsType<Route>(masters.Single(o => o.Id == RouteId)).RouteState);
        // 追跡付きの更新用取得(GetStateByIds)は使わない
        _routeRepositoryMock.Verify(r => r.GetStateByIds(
            It.IsAny<IEnumerable<ulong>>(), It.IsAny<CancellationToken>()), Times.Never);
        _routeRepositoryMock.Verify(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>()), Times.Once);
        _leverRepositoryMock.Verify(r => r.GetAllStatesForBroadcast(It.IsAny<CancellationToken>()), Times.Once);
    }
}
