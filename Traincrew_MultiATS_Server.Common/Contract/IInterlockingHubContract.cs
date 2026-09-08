using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface IInterlockingHubContract
{
    Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData);
    Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData);
    Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData);
}

public interface IInterlockingClientContract
{
    Task ReceiveData(DataToInterlocking data);
    Task ReceiveSignalData(List<SignalData> signalData);
}
