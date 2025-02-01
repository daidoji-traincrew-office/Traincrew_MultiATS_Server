using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Protection;

namespace Traincrew_MultiATS_Server.Services;

public class ProtectionService(IProtectionRepository protectionRepository)
{
	public async Task<bool> GetProtectionZoneStateByBougoZone(List<int> bougo_zone)
	{
		return await protectionRepository.GetProtectionZoneState(bougo_zone);
	}
}