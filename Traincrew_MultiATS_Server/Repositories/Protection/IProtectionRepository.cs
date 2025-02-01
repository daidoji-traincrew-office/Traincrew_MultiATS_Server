public interface IProtectionRepository
{
	Task<bool> GetProtectionZoneState(List<int> bougo_zone);
}