public interface IProtectionRepository
{
	Task EnableProtection(string trainNumber, List<int> protectionZones);
	Task DisableProtection(string trainNumber);
}