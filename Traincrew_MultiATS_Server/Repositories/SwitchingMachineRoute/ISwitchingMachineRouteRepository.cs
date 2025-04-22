namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

public interface ISwitchingMachineRouteRepository
{
    Task<List<Models.SwitchingMachineRoute>> GetAll();
}