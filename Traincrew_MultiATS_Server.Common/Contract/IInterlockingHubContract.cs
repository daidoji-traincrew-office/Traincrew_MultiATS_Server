using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface IInterlockingHubContract
{
    Task SetPhysicalLeverData(InterlockingLeverData leverData);
    Task<bool> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData);
    Task SetDestinationButtonState(DestinationButtonData buttonData);
}

public interface IInterlockingClientContract
{
    Task ReceiveData(DataToInterlocking data);
}
