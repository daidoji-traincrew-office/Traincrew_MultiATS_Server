namespace Traincrew_MultiATS_Server.Repositories.TtcWindowLinkRouteCondition;

public interface ITtcWindowLinkRouteConditionRepository
{
    /// <summary>
    /// TtcWindowLinkRouteConditionを追加する
    /// </summary>
    /// <param name="routeCondition">追加するTtcWindowLinkRouteCondition</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task AddAsync(Models.TtcWindowLinkRouteCondition routeCondition, CancellationToken cancellationToken = default);
}
