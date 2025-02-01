using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Protection;

namespace Traincrew_MultiATS_Server.Services;

public class ProtectionService(IProtectionRepository protectionRepository)
{
	public async Task<bool> GetProtectionZoneStateByBougoZone(List<int> bougo_zone)
	{
		return await protectionRepository.GetProtectionZoneState(bougo_zone);
	}

	public async Task EnableProtection(string train_number, int protection_zone)
	{
		await protectionRepository.EnableProtection(train_number, protection_zone);
	}

	public async Task DisableProtection(string train_number)
	{
		await protectionRepository.DisableProtection(train_number);
	}
}