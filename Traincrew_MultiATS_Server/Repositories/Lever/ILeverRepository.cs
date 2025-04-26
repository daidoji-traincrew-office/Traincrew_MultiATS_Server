namespace Traincrew_MultiATS_Server.Repositories.Lever;

public interface ILeverRepository
{
    Task<Models.Lever?> GetLeverByNameWithState(string name);

    Task<List<ulong>?> GetAllIds();

}