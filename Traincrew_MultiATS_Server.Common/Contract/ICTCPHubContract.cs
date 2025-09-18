using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ICTCPHubContract
{
    Task<DataToCTCP> SendData_CTCP(List<string> activeStationsList);
    Task<InterlockingLeverData> SetCTCLeverData(InterlockingLeverData leverData);
}

public interface ICTCPClientContract
{
    Task ReceiveData(DataToCTCP data);
}