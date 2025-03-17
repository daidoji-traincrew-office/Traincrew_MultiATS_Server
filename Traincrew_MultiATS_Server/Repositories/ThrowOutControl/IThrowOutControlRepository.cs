namespace Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

public interface IThrowOutControlRepository
{
    Task<List<Models.ThrowOutControl>> GetAll();
    Task<List<Models.ThrowOutControl>> GetBySourceRouteIds(List<ulong> sourceRouteIds);
    Task<List<Models.ThrowOutControl>> GetByTargetRouteIds(List<ulong> targetRouteIds);
}