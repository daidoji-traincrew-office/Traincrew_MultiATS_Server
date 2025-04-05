namespace Traincrew_MultiATS_Server.Repositories.Route;

public interface IRouteRepository
{
    /// <summary>
    /// てこ反応リレーが落下しており かつ 進路照査リレーが扛上している進路に対し、進路照査リレーを落下させる
    /// </summary>
    Task DropRouteRelayWhereLeverRelayIsDropped();
    /// <summary>
    /// てこ反応リレーが扛上しているすべての進路IDを取得する 
    /// </summary>
    /// <returns> てこ反応リレーが扛上している進路のリスト </returns>
    Task<List<ulong>> GetIdsWhereLeverRelayIsRaised();
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
    /// </summary>
    /// <returns> 進路鎖錠または接近鎖状の掛かっている進路のリスト </returns>
    Task<List<ulong>> GetIdsWhereRouteLockRelayOrApproachLockMRIsRaised();
    /// <summary>
    /// 進路照査リレーが扛上している または 接近鎖状の掛かっているすべての進路IDを取得する
    /// </summary>
    /// <returns> 進路照査リレーが扛上している または 接近鎖状の掛かっている進路のリスト </returns>
    Task<List<ulong>> GetIdsWhereRouteRelayOrApproachLockMRIsRaised();
}