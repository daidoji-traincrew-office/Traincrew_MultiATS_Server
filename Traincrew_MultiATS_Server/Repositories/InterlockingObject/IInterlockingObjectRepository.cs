
namespace Traincrew_MultiATS_Server.Repositories.InterlockingObject;

public interface IInterlockingObjectRepository
{
    public Task GetObjectByIds(IEnumerable<ulong> ids);
    public Task<Models.InterlockingObject> GetObject(string stationId, string name);
}