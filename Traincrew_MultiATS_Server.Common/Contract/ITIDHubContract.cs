using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ITIDHubContract
{
    Task<ConstantDataToTID> SendData_TID();
}

public interface ITIDClientContract
{
    // クライアント側のメソッド定義は不要
}