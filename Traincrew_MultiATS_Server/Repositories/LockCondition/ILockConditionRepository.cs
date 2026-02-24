using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.LockCondition;

public interface ILockConditionRepository
{
   // public Task<List<Models.LockCondition>> GetConditionsByObjectIdAndType(ulong objectId, LockType type);
   public Task<Dictionary<ulong, List<Models.LockCondition>>> GetConditionsByType(LockType type);
   public Task<Dictionary<ulong, List<Models.LockCondition>>> GetConditionsByObjectIdsAndType(List<ulong> objectIds, LockType type);
   Task DeleteAll();

   /// <summary>LockConditionを追加しSaveChanges（IDを確定して返す）</summary>
   Task<Models.LockCondition> AddAndSaveAsync(
       Models.LockCondition entity,
       CancellationToken cancellationToken = default);

   /// <summary>LockConditionObjectを追加しSaveChanges</summary>
   Task AddObjectAndSaveAsync(
       LockConditionObject entity,
       CancellationToken cancellationToken = default);

   Task<Dictionary<ulong, List<Models.LockCondition>>> GetConditionsByApproachAlertConditionIds(
       List<ulong> ids);
}
