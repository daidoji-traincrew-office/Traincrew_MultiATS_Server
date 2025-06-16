using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ITrainHubContract
{
    Task<ServerToATSData> SendData_ATS(AtsToServerData clientData);
    Task DriverGetsOff(string trainNumber);
}

public interface ITrainClientContract
{
    // クライアント側のメソッド定義は不要
}