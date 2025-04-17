namespace Traincrew_MultiATS_Server.Repositories.Route;

public interface IRouteRepository
{
    /// <summary>
    /// IDから進路を取得する
    /// </summary>
    Task<List<Models.Route>> GetByIdsWithState(List<ulong> ids);
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

}