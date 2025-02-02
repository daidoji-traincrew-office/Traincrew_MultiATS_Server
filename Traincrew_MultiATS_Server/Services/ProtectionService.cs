using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Services;

public class ProtectionService(IProtectionRepository protectionRepository)
{
	public async Task<bool> IsProtectionEnabledForTrackCircuits(List<TrackCircuit> trackCircuits)
	{
		// 防護範囲の最大、最小を求め、それの+1、-1を求める
		var protectionZone = trackCircuits.Select(tc => tc.ProtectionZone).ToList();
		var minProtectionZone = protectionZone.Min() - 1;
		var maxProtectionZone = protectionZone.Max() + 1;
		// その防護範囲で防護無線が発報されているか確認
		return await protectionRepository.IsProtectionEnabled(minProtectionZone, maxProtectionZone);
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