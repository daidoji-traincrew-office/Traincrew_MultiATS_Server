namespace Traincrew_MultiATS_Server.Repositories.ApproachAlertCondition;

public interface IApproachAlertConditionRepository
{
    /// <summary>エンティティを追加しSaveChanges（IDを確定して返す）</summary>
    Task<Models.ApproachAlertCondition> AddAndSaveAsync(
        Models.ApproachAlertCondition entity,
        CancellationToken cancellationToken = default);

    /// <summary>全件削除</summary>
    Task DeleteAll(CancellationToken cancellationToken = default);

    Task<List<Models.ApproachAlertCondition>> GetByStationIdAndIsUpPairs(
        List<(string StationId, bool IsUp)> pairs);
}
