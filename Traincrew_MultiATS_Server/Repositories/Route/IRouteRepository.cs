namespace Traincrew_MultiATS_Server.Repositories.Route;

public interface IRouteRepository
{
    /// <summary>
    /// IDから進路を取得する
    /// </summary>
    Task<List<Models.Route>> GetByIdsWithState(List<ulong> ids);
    /// <summary>
    /// てこ反応リレーまたは総括制御XRリレー、総括制御YSリレーが扛上している進路のIDを取得する
    /// </summary>
    /// <returns>てこ反応リレーまたは総括制御XRリレー、総括制御YSリレーが扛上している進路のIDのリスト</returns>
    Task<List<ulong>> GetIdsWhereLeverRelayOrThrowOutIsRaised();
    /// <summary>
    /// てこか倒れているか、着点ボタンが圧下している進路のIDを取得する
    /// </summary>
    /// <returns>進路のIDのリスト</returns>
    Task<List<ulong>> GetIdsToOpen();
    /// <summary>
    /// てこ反応リレーが落下しており かつ 転てつ器無し進路照査リレーが扛上している進路に対し、転てつ器無し進路照査リレーを落下させる
    /// </summary>
    Task DropRouteRelayWithoutSwitchingMachineWhereLeverRelayIsDropped();
    /// <summary>
    /// 転てつ器無し進路照査リレーが落下しており かつ 進路照査リレーが扛上している進路に対し、進路照査リレーを落下させる
    /// </summary>
    Task DropRouteRelayWhereRouteRelayWithoutSwitchingMachineIsDropped();
    /// <summary>
    /// てこ反応リレーが扛上しているすべての進路IDを取得する 
    /// </summary>
    /// <returns> てこ反応リレーが扛上している進路のリスト </returns>
    Task<List<ulong>> GetIdsWhereLeverRelayIsRaised();
    /// <summary>
    /// てこ反応リレーが扛上しているすべての進路IDを取得する 
    /// </summary>
    /// <returns> てこ反応リレーが扛上している進路のリスト </returns>
    Task<List<ulong>> GetIdsWhereRouteRelayWithoutSwitchingMachineIsRaised();
    /// <summary>
    /// 進路照査リレーが落下しており かつ 信号制御リレーが扛上している進路に対し、信号制御リレーを落下させる
    /// </summary>
    Task DropSignalRelayWhereRouteRelayIsDropped();
    /// <summary>
    /// 進路照査リレーが扛上しているすべての進路IDを取得する
    /// </summary>
    /// <returns> 進路照査リレーが扛上している進路のリスト </returns>
    Task<List<ulong>> GetIdsWhereRouteRelayIsRaised();
    /// <summary>
    /// 進路鎖錠または接近鎖状の掛かっているすべての進路IDを取得する
    /// 具体的には以下の条件に該当するリレーを取得する
    /// - 進路鎖錠リレーが落下している
    /// - 接近鎖状MRリレーが落下している
    /// </summary>
    /// <returns> 進路鎖錠または接近鎖状の掛かっている進路のリスト </returns>
    Task<List<ulong>> GetIdsForRouteLockRelay();
    /// <summary>
    /// 進路照査リレーが扛上している または 接近鎖状の掛かっているすべての進路IDを取得する
    /// 具体的には以下の条件に該当するリレーを取得する
    /// - 進路照査リレーが落下している
    /// - 接近鎖状MRリレーが落下している
    /// - 接近鎖状MSリレーが扛上している
    /// </summary>
    /// <returns> 進路照査リレーが扛上している または 接近鎖状の掛かっている進路のリスト </returns>
    Task<List<ulong>> GetIdsForApproachLockRelay();
    /// <summary>
    /// 接近鎖錠MSリレーが扛上しているすべての進路を取得する
    /// </summary>
    /// <returns> 接近鎖錠MSリレーが扛上している進路のリスト </returns>
    Task<List<Models.Route>> GetWhereApproachLockMSRelayIsRaised();
    /// <summary>
    /// 渡されたrouteIdsの中から、Sリレーが落下している又は進路照査リレーが扛上している進路の進路IDを取得する
    /// </summary>
    /// <param name="routeIds">チェック対象の進路IDリスト</param>
    /// <returns>Sリレーが落下している又は進路照査リレーが扛上している進路のIDリスト</returns>
    Task<List<ulong>> GetIdsByIdsForSRelay(List<ulong> routeIds);
    /// <summary>
    /// 指定されたID以外の進路の総括制御Sリレーを落下させる
    /// </summary>
    /// <param name="targetIds">総括制御Sリレーを維持する進路のIDリスト</param>
    Task DropThrowOutSRelayExceptByIds(List<ulong> targetIds);
}