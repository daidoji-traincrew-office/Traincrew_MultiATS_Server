using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Protection;

public class ProtectionRepository(ApplicationDbContext context) : IProtectionRepository
{
	public async Task<bool> GetProtectionZoneState(List<int> bougo_zone)
	{
		List<ProtectionZoneState> protectionZoneStates = 
			await context.protectionZoneStates.ToListAsync();
		if (protectionZoneStates == null)
			return false;
		else
		{
			foreach (var item in protectionZoneStates)
			{
				foreach(var item2 in bougo_zone)
				{
					if (item2 == item.protection_zone)
						return true;
				}
			}
			return false;
		}
	}
}