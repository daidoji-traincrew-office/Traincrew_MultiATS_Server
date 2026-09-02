using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Repositories.Route;

public interface IRouteRepository
{
    /// <summary>
    /// IDから進路を取得する
    /// </summary>
    Task<List<Models.Route>> GetByIdsWithState(List<ulong> ids);

    /// <summary>
    /// TcNameから進路を取得する
    /// </summary>
    Task<List<Models.Route>> GetByTcNameWithState(string tcName);

    /// <summary>
    /// 駅IDから進路を取得する
    /// </summary>
    Task<List<Models.Route>> GetByStationIds(List<string> stationIds);
    /// <summary>
    /// 進路確保中のすべての進路IDを取得する
    /// </summary>
    /// <returns> 進路確保中の進路のリスト </returns>
    Task<List<ulong>> GetIdsWhereRouteSecured();

    /// <summary>
    /// すべての進路IDを取得する
    /// </summary>
    /// <returns>全ての進路のリスト</returns>
    Task<List<ulong>> GetIdsForAll();

    /// <summary>
    /// 進路名から進路IDへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>進路名をキー、進路IDを値とする辞書</returns>
    Task<Dictionary<string, ulong>> GetAllIdForName(CancellationToken cancellationToken = default);

    /// <summary>
    /// 進路名から進路エンティティへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>進路名をキー、進路エンティティを値とする辞書</returns>
    Task<Dictionary<string, Models.Route>> GetByNames(CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定したIDのRouteStateのIsSignalControlRaisedのみを更新する
    /// (route_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="ids">進路のIDリスト</param>
    /// <param name="isSignalControlRaised">信号制御リレー状態</param>
    Task SetIsSignalControlRaisedByIds(List<ulong> ids, RaiseDrop isSignalControlRaised);

    /// <summary>
    /// 指定したIDのRouteStateのIsRouteSecuredのみを更新する
    /// (route_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="ids">進路のIDリスト</param>
    /// <param name="isRouteSecured">進路確保中か</param>
    Task SetIsRouteSecuredByIds(List<ulong> ids, RaiseDrop isRouteSecured);

    /// <summary>
    /// 指定したIDのRouteStateのIsCtcControlledのみを更新する
    /// (route_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="ids">進路のIDリスト</param>
    /// <param name="isCtcControlled">CTC制御中か</param>
    Task SetIsCtcControlledByIds(List<ulong> ids, RaiseDrop isCtcControlled);
}
