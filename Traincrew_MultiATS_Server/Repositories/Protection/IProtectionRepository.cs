using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Protection;

public interface IProtectionRepository
{
	Task EnableProtection(string trainNumber, List<int> protectionZones);
	Task DisableProtection(string trainNumber);
	Task<List<ProtectionZoneState>> GetProtectionZoneStates();
	Task DeleteById(ulong id);
}