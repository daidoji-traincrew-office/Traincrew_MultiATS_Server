namespace Traincrew_MultiATS_Server.Repositories.SignalType;

public interface ISignalTypeRepository
{
    Task<List<string>> GetAllNames(CancellationToken cancellationToken = default);
}
