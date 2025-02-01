public interface IProtectionRepository
{
	Task<bool> GetProtectionZoneState(List<int> bougo_zone);
	Task EnableProtection(string train_number, int bougo_zone);
	Task DisableProtection(string train_number);
}