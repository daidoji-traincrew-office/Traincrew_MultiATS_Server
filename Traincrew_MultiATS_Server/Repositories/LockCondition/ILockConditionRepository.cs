namespace Traincrew_MultiATS_Server.Repositories.LockCondition;

public interface ILockConditionRepository
{
   public Task<List<Models.LockCondition>> GetConditionsByObjectIdAndType(ulong objectId, string type);
}