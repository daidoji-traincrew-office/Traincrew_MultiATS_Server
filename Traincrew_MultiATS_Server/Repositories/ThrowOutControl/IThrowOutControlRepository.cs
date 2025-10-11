using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

public interface IThrowOutControlRepository
{
    Task<List<Models.ThrowOutControl>> GetAll();
    Task<List<Models.ThrowOutControl>> GetBySourceIds(List<ulong> sourceIds);
    Task<List<Models.ThrowOutControl>> GetBySourceIdsAndTypes(List<ulong> sourceIds, List<ThrowOutControlType> controlTypes);
    Task<List<Models.ThrowOutControl>> GetByTargetIds(List<ulong> targetIds);
    Task<List<Models.ThrowOutControl>> GetByTargetIdsAndTypes(List<ulong> targetIds, List<ThrowOutControlType> controlTypes);
    Task<List<Models.ThrowOutControl>> GetBySourceAndTargetIds(List<ulong> ids);
    Task<List<Models.ThrowOutControl>> GetByControlTypes(List<ThrowOutControlType> controlTypes);
}