namespace Traincrew_MultiATS_Server.Repositories.ApproachAlertState;

public interface IApproachAlertStateRepository
{
    Task<List<ulong>> GetIdsWhereShouldRing();
    Task<List<ulong>> GetIdsWhereHasShortCircuitedCondition();
    Task<List<Models.ApproachAlertState>> GetByIds(List<ulong> ids);
    Task SetIsRingingFalseByStationIdAndIsUp(string stationId, bool isUp);
    Task<List<Models.ApproachAlertState>> GetWhereIsRinging();
}
