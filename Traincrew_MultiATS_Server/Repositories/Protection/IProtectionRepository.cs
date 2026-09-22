using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Protection;

public interface IProtectionRepository
{
	Task Enable(string trainNumber, List<int> protectionZones);
	Task Disable(string trainNumber);
	Task<List<ProtectionZoneState>> GetAll();
	Task DeleteById(ulong id);
}