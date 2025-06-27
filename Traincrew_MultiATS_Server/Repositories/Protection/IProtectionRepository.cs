using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Protection;

public interface IProtectionRepository
{
	Task<bool> IsProtectionEnabled(int minProtectionZone, int maxProtectionZone);
	Task EnableProtection(string trainNumber, List<int> protectionZones);
	Task DisableProtection(string trainNumber);
	Task<List<ProtectionZoneState>> GetProtectionZoneStates(string trainNumber);
	Task DeleteById(ulong id);
}