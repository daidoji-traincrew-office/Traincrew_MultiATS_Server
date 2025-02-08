public interface IProtectionRepository
{
	Task<bool> IsProtectionEnabled(int minProtectionZone, int maxProtectionZone);
	Task EnableProtection(string trainNumber, List<int> protectionZones);
	Task DisableProtection(string trainNumber);
}