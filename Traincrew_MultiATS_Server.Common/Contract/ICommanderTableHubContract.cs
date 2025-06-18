using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ICommanderTableHubContract
{
    Task<DataToCommanderTable> SendData_CommanderTable();
    Task SendTroubleData(TroubleData troubleData);
    Task SendOperationNotificationData(OperationNotificationData operationNotificationData);
    Task SendTrackCircuitData(TrackCircuitData trackCircuitData);
    Task DeleteTrain(string trainName);
    Task AddOperationInformation(OperationInformationData operationInformationData);
    Task UpdateOperationInformation(OperationInformationData operationInformationData);
    Task<List<OperationInformationData>> GetAllOperationInformations();
    Task DeleteOperationInformation(long id);
}

public interface ICommanderTableClientContract
{
    // クライアント側のメソッド定義は不要
}
