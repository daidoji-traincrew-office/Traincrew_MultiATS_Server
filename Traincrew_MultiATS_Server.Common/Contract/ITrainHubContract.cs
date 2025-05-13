using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ITrainHubContract
{
    Task<DataFromServer> SendData_ATS(DataToServer clientData);
}

public interface ITrainClientContract
{
    // クライアント側のメソッド定義は不要
}