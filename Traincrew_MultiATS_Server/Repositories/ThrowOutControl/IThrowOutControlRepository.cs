namespace Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

public interface IThrowOutControlRepository
{
    Task<List<Models.ThrowOutControl>> GetAll();
    Task<List<Models.ThrowOutControl>> GetBySourceIds(List<ulong> sourceIds);
    Task<List<Models.ThrowOutControl>> GetByTargetIds(List<ulong> targetIds);
}