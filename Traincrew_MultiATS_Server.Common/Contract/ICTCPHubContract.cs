using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ICTCPHubContract
{
    Task<DataToCTCP> SendData_CTCP();
    Task<RouteData> SetCtcRelay(string TcName, RaiseDrop raiseDrop);
}

public interface ICTCPClientContract
{
    Task ReceiveData(DataToCTCP data);
}