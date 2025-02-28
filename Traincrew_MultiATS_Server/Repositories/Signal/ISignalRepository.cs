namespace Traincrew_MultiATS_Server.Repositories.Signal;

public interface ISignalRepository
{
    Task<List<Models.Signal>> GetAll();
    Task<List<Models.Signal>> GetSignalsByNamesForCalcIndication(List<string> signalNames);
    Task<List<string>> GetSignalNamesByTrackCircuits(List<string> trackCircuitNames, bool isUp);
}