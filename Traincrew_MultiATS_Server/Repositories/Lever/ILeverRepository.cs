namespace Traincrew_MultiATS_Server.Repositories.Lever;

public interface ILeverRepository
{
    Task<List<ulong>> GetIdsBySwitchingMachineIds(List<ulong> ids);

    Task<Models.Lever?> GetLeverByNameWithState(string name);

    Task<List<ulong>?> GetAllIds();
    
    Task<List<Models.Lever>> GetAllWithState();

}