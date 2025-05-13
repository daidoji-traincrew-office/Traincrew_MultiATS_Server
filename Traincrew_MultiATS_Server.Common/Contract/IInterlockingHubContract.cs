using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface IInterlockingHubContract
{
    Task<DataToInterlocking> SendData_Interlocking(List<string> activeStationsList);
    Task SetPhysicalLeverData(InterlockingLeverData leverData);
    Task<bool> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData);
    Task SetDestinationButtonState(DestinationButtonData buttonData);
}

public interface IInterlockingClientContract
{
    // クライアント側のメソッド定義は不要
}
