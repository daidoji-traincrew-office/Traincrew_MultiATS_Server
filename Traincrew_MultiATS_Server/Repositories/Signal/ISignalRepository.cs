namespace Traincrew_MultiATS_Server.Repositories.Signal;

public interface ISignalRepository
{
    Task<List<Models.Signal>> GetSignalsByNamesForCalcIndication(List<string> signalNames);
}