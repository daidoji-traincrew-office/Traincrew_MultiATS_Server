using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 連動オブジェクト(interlocking_object の7サブタイプ)を、状態込みで取得するローダー。
/// マスタ部分(サブタイプごとの非可変フィールド)は起動時イーガーロード済みの
/// <see cref="IInterlockingObjectMasterStore"/> から複製し、状態部分だけを
/// 含まれるサブタイプごとに1回、対応するリポジトリの GetStateByIds でまとめて取得して差し込む。
/// (RendoService/SwitchingMachineServiceがtickごとに全マスタをJoinして取得していたのをやめるための置き換え)
/// </summary>
public interface IInterlockingObjectStateLoader
{
    /// <summary>
    /// 指定したIDの連動オブジェクトを、状態込みで取得する。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 呼び出し側の不変条件: 1メソッド呼び出し内で、同じIDに対応する連動オブジェクト(の State)は
    /// 必ず1つのインスタンスにすること。このメソッドを同じ処理の中で複数回呼び、重なりうるIDを
    /// 別々に取得しては<strong>ならない</strong>(片方だけ更新・Saveして、もう片方が古い値を読む
    /// 事故につながる)。対象IDは呼び出し前に和集合にまとめ、1回だけ呼ぶこと。
    /// </para>
    /// <para>
    /// 返すインスタンスは <see cref="IInterlockingObjectMasterStore"/> の複製であり、
    /// マスタスナップショットそのものではない。ただし複製を
    /// <c>IGeneralRepository.Save</c> 等に渡してはならない点は共有インスタンスと同様
    /// (マスタ部分がState抜きで書き戻され、意図しない上書きが起きる)。
    /// </para>
    /// </remarks>
    Task<Dictionary<ulong, InterlockingObject>> GetByIdsWithState(IEnumerable<ulong> ids);

    /// <summary>
    /// 全連動オブジェクトを、状態込みで取得する。<b>配信(読み取り専用)専用</b>。
    /// </summary>
    /// <remarks>
    /// 状態は各リポジトリの GetAllStatesForBroadcast(AsNoTracking)でサブタイプごとに1回だけ取得する。
    /// 呼び出し側の不変条件(同じIDのStateは1インスタンス)は GetByIdsWithState と同じで、
    /// 1回の配信処理につき1回だけ呼び、結果を共有すること。返すエンティティは更新・Saveに使ってはならない。
    /// 並びはIDの昇順。
    /// </remarks>
    Task<InterlockingObjectsWithState> GetAllWithStateForBroadcast();
}

/// <summary>
/// <see cref="IInterlockingObjectStateLoader.GetAllWithStateForBroadcast"/> の結果。
/// 各リストはID昇順で、状態を載せた複製を持つ(マスタスナップショットの共有インスタンスではない)。
/// </summary>
public sealed class InterlockingObjectsWithState(
    IReadOnlyList<Route> routes,
    IReadOnlyList<SwitchingMachine> switchingMachines,
    IReadOnlyList<TrackCircuit> trackCircuits,
    IReadOnlyList<Lever> levers,
    IReadOnlyList<DirectionRoute> directionRoutes,
    IReadOnlyList<DirectionSelfControlLever> directionSelfControlLevers,
    IReadOnlyList<RouteCentralControlLever> routeCentralControlLevers)
{
    public IReadOnlyList<Route> Routes { get; } = routes;
    public IReadOnlyList<SwitchingMachine> SwitchingMachines { get; } = switchingMachines;
    public IReadOnlyList<TrackCircuit> TrackCircuits { get; } = trackCircuits;
    public IReadOnlyList<Lever> Levers { get; } = levers;
    public IReadOnlyList<DirectionRoute> DirectionRoutes { get; } = directionRoutes;
    public IReadOnlyList<DirectionSelfControlLever> DirectionSelfControlLevers { get; } = directionSelfControlLevers;
    public IReadOnlyList<RouteCentralControlLever> RouteCentralControlLevers { get; } = routeCentralControlLevers;

    private readonly Lazy<IReadOnlyDictionary<ulong, Route>> _routesById =
        new(() => routes.ToDictionary(r => r.Id));

    private readonly Lazy<IReadOnlyDictionary<ulong, TrackCircuit>> _trackCircuitsById =
        new(() => trackCircuits.ToDictionary(t => t.Id));

    private readonly Lazy<IReadOnlyDictionary<ulong, DirectionRoute>> _directionRoutesById =
        new(() => directionRoutes.ToDictionary(d => d.Id));

    public IReadOnlyDictionary<ulong, Route> RoutesById => _routesById.Value;
    public IReadOnlyDictionary<ulong, TrackCircuit> TrackCircuitsById => _trackCircuitsById.Value;
    public IReadOnlyDictionary<ulong, DirectionRoute> DirectionRoutesById => _directionRoutesById.Value;
}

public class InterlockingObjectStateLoader(
    IInterlockingObjectMasterStore masterStore,
    IRouteRepository routeRepository,
    ISwitchingMachineRepository switchingMachineRepository,
    ITrackCircuitRepository trackCircuitRepository,
    ILeverRepository leverRepository,
    IDirectionRouteRepository directionRouteRepository,
    IDirectionSelfControlLeverRepository directionSelfControlLeverRepository,
    IRouteCentralControlLeverRepository routeCentralControlLeverRepository
) : IInterlockingObjectStateLoader
{
    public async Task<Dictionary<ulong, InterlockingObject>> GetByIdsWithState(IEnumerable<ulong> ids)
    {
        // GetByIdsが返すのは常駐スナップショットの共有インスタンスそのもの(複製ではない)。
        // 複製は後段のCloneWithStateで作る。重複IDはGetByIds内で畳まれる。
        // ここではまだ状態を載せていないので書き換え禁止のまま。
        var masters = masterStore.Current.GetByIds<InterlockingObject>(ids);

        // 含まれるサブタイプだけ、状態をまとめて取得する(含まれない型はクエリしない)
        Dictionary<ulong, RouteState>? routeStates = null;
        Dictionary<ulong, SwitchingMachineState>? switchingMachineStates = null;
        Dictionary<ulong, TrackCircuitState>? trackCircuitStates = null;
        Dictionary<ulong, LeverState>? leverStates = null;
        Dictionary<ulong, DirectionRouteState>? directionRouteStates = null;
        Dictionary<ulong, DirectionSelfControlLeverState>? directionSelfControlLeverStates = null;
        Dictionary<ulong, RouteCentralControlLeverState>? routeCentralControlLeverStates = null;

        var routeIds = masters.OfType<Route>().Select(o => o.Id).ToList();
        if (routeIds.Count > 0)
        {
            routeStates = (await routeRepository.GetStateByIds(routeIds)).ToDictionary(s => s.Id);
        }

        var switchingMachineIds = masters.OfType<SwitchingMachine>().Select(o => o.Id).ToList();
        if (switchingMachineIds.Count > 0)
        {
            switchingMachineStates =
                (await switchingMachineRepository.GetStateByIds(switchingMachineIds)).ToDictionary(s => s.Id);
        }

        var trackCircuitIds = masters.OfType<TrackCircuit>().Select(o => o.Id).ToList();
        if (trackCircuitIds.Count > 0)
        {
            trackCircuitStates = (await trackCircuitRepository.GetStateByIds(trackCircuitIds)).ToDictionary(s => s.Id);
        }

        var leverIds = masters.OfType<Lever>().Select(o => o.Id).ToList();
        if (leverIds.Count > 0)
        {
            leverStates = (await leverRepository.GetStateByIds(leverIds)).ToDictionary(s => s.Id);
        }

        var directionRouteIds = masters.OfType<DirectionRoute>().Select(o => o.Id).ToList();
        if (directionRouteIds.Count > 0)
        {
            directionRouteStates =
                (await directionRouteRepository.GetStateByIds(directionRouteIds)).ToDictionary(s => s.Id);
        }

        var directionSelfControlLeverIds = masters.OfType<DirectionSelfControlLever>().Select(o => o.Id).ToList();
        if (directionSelfControlLeverIds.Count > 0)
        {
            directionSelfControlLeverStates =
                (await directionSelfControlLeverRepository.GetStateByIds(directionSelfControlLeverIds))
                .ToDictionary(s => s.Id);
        }

        var routeCentralControlLeverIds = masters.OfType<RouteCentralControlLever>().Select(o => o.Id).ToList();
        if (routeCentralControlLeverIds.Count > 0)
        {
            routeCentralControlLeverStates =
                (await routeCentralControlLeverRepository.GetStateByIds(routeCentralControlLeverIds))
                .ToDictionary(s => s.Id);
        }

        var result = new Dictionary<ulong, InterlockingObject>(masters.Count);
        foreach (var master in masters)
        {
            // マスタの複製に、対応する状態を載せる。CloneWithStateは内部でMemberwiseCloneするため、
            // masterStoreが持つ共有インスタンス自体は書き換わらない。
            // SwitchingMachine/Lever/TrackCircuitはCloneWithStateが非null引数のため、
            // 対応する状態行が無かった場合(通常は起きない想定)はnull!を渡す。
            // これは旧LEFT JOIN実装がnullを載せていたのと同じ挙動。
            result[master.Id] = master switch
            {
                Route r => r.CloneWithState(routeStates?.GetValueOrDefault(r.Id)),
                SwitchingMachine sm => sm.CloneWithState(switchingMachineStates?.GetValueOrDefault(sm.Id)!),
                TrackCircuit tc => tc.CloneWithState(trackCircuitStates?.GetValueOrDefault(tc.Id)!),
                Lever lv => lv.CloneWithState(leverStates?.GetValueOrDefault(lv.Id)!),
                DirectionRoute dr => dr.CloneWithState(directionRouteStates?.GetValueOrDefault(dr.Id)),
                DirectionSelfControlLever dscl =>
                    dscl.CloneWithState(directionSelfControlLeverStates?.GetValueOrDefault(dscl.Id)),
                RouteCentralControlLever rccl =>
                    rccl.CloneWithState(routeCentralControlLeverStates?.GetValueOrDefault(rccl.Id)),
                _ => throw new InvalidOperationException($"未知の連動オブジェクトサブタイプです: {master.GetType()}")
            };
        }

        return result;
    }

    public async Task<InterlockingObjectsWithState> GetAllWithStateForBroadcast()
    {
        // ティックの開始時に1回だけ読む(途中で読み直さない)
        var master = masterStore.Current;

        var routeStates = (await routeRepository.GetAllStatesForBroadcast()).ToDictionary(s => s.Id);
        var switchingMachineStates =
            (await switchingMachineRepository.GetAllStatesForBroadcast()).ToDictionary(s => s.Id);
        var trackCircuitStates = (await trackCircuitRepository.GetAllStatesForBroadcast()).ToDictionary(s => s.Id);
        var leverStates = (await leverRepository.GetAllStatesForBroadcast()).ToDictionary(s => s.Id);
        var directionRouteStates = (await directionRouteRepository.GetAllStatesForBroadcast()).ToDictionary(s => s.Id);
        var directionSelfControlLeverStates =
            (await directionSelfControlLeverRepository.GetAllStatesForBroadcast()).ToDictionary(s => s.Id);
        var routeCentralControlLeverStates =
            (await routeCentralControlLeverRepository.GetAllStatesForBroadcast()).ToDictionary(s => s.Id);

        // masterStore.Current.All はID昇順。状態行が無い場合は null を載せる(旧LEFT JOIN実装・GetByIdsWithStateと同じ)
        return new(
            master.All.OfType<Route>()
                .Select(r => r.CloneWithState(routeStates.GetValueOrDefault(r.Id))).ToList(),
            master.All.OfType<SwitchingMachine>()
                .Select(sm => sm.CloneWithState(switchingMachineStates.GetValueOrDefault(sm.Id)!)).ToList(),
            master.All.OfType<TrackCircuit>()
                .Select(tc => tc.CloneWithState(trackCircuitStates.GetValueOrDefault(tc.Id)!)).ToList(),
            master.All.OfType<Lever>()
                .Select(lv => lv.CloneWithState(leverStates.GetValueOrDefault(lv.Id)!)).ToList(),
            master.All.OfType<DirectionRoute>()
                .Select(dr => dr.CloneWithState(directionRouteStates.GetValueOrDefault(dr.Id))).ToList(),
            master.All.OfType<DirectionSelfControlLever>()
                .Select(dscl => dscl.CloneWithState(directionSelfControlLeverStates.GetValueOrDefault(dscl.Id)))
                .ToList(),
            master.All.OfType<RouteCentralControlLever>()
                .Select(rccl => rccl.CloneWithState(routeCentralControlLeverStates.GetValueOrDefault(rccl.Id)))
                .ToList());
    }
}
