namespace Traincrew_MultiATS_Server.Repositories.Lever;

public interface ILeverRepository
{
    Task<Models.Lever?> GetLeverByNameWitState(string name);
    Task<List<ulong>> GetAllDirectionLeverIds();
}