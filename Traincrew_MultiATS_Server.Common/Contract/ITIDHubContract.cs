using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ITIDHubContract
{
}

public interface ITIDClientContract
{
    Task ReceiveData(ConstantDataToTID data);
    Task ReceiveSignalData(List<SignalData> signalData);
}