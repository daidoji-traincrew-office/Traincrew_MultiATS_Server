using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Services;

public class ProtectionService(IProtectionRepository protectionRepository)
{
	public async Task<bool> IsProtectionEnabledForTrackCircuits(List<TrackCircuit> trackCircuits)
	{
		throw new NotImplementedException();
	}
	
	public async Task EnableProtectionByTrackCircuits(string trainNumber, List<TrackCircuit> trackCircuits)
	{
		await protectionRepository.EnableProtection(
			trainNumber, trackCircuits.Select(tc => tc.ProtectionZone).ToList());
	}

	public async Task DisableProtection(string trainNumber)
	{
		await protectionRepository.DisableProtection(trainNumber);
	}
}