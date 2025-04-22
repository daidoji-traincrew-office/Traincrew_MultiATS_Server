using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Lock;

public interface ILockRepository
{
   Task<Dictionary<ulong, List<Models.Lock>>> GetByObjectIdsAndType(List<ulong> objectIds, LockType type);
}