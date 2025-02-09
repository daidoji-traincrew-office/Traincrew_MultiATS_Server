using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.LockCondition;

public interface ILockConditionRepository
{
   // public Task<List<Models.LockCondition>> GetConditionsByObjectIdAndType(ulong objectId, LockType type);
   public Task<Dictionary<ulong, List<Models.LockCondition>>> GetConditionsByType(LockType type);
   public Task<Dictionary<ulong, List<Models.LockCondition>>> GetConditionsByObjectIdsAndType(List<ulong> objectIds, LockType type);
}