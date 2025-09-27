namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

public interface ISwitchingMachineRouteRepository
{
    Task<List<Models.SwitchingMachineRoute>> GetAll();
    Task<List<Models.SwitchingMachineRoute>> GetBySwitchingMachineIds(List<ulong> switchingMachineIds);
}