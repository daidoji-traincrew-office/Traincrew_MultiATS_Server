namespace Traincrew_MultiATS_Server.Repositories.NextSignal;

public interface INextSignalRepository
{
    public Task<List<Models.NextSignal>> GetNextSignalByNamesOrderByDepthDesc(List<string> signalNames);
    public Task<List<Models.NextSignal>> GetByNamesAndDepth(List<string> signalNames, int depth);
    public Task<List<Models.NextSignal>> GetAllByDepth(int depth);
}