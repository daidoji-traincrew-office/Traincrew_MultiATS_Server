namespace Traincrew_MultiATS_Server.Repositories.SignalType;

public interface ISignalTypeRepository
{
    Task<HashSet<string>> GetAllNames(CancellationToken cancellationToken = default);
}
