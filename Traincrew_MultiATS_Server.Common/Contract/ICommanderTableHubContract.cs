using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ICommanderTableHubContract
{
    Task<DataToCommanderTable> SendData_CommanderTable();
    Task SendTroubleData(TroubleData troubleData);
    Task SendOperationNotificationData(OperationNotificationData operationNotificationData);
    Task SendTrackCircuitData(TrackCircuitData trackCircuitData);
    Task DeleteTrain(string trainName);
    Task<OperationInformationData> AddOperationInformation(OperationInformationData operationInformationData);
    Task<OperationInformationData> UpdateOperationInformation(OperationInformationData operationInformationData);
    Task<List<OperationInformationData>> GetAllOperationInformations();
    Task DeleteOperationInformation(long id);
    Task AddProtectionZoneState(ProtectionRadioData protectionRadioData);
    Task UpdateProtectionZoneState(ProtectionRadioData protectionRadioData);
    Task DeleteProtectionZoneState(ulong id);
    Task<List<ProtectionRadioData>> GetProtectionZoneStates();
    Task<List<TrainStateData>> GetAllTrainState();
    Task<TrainStateData> UpdateTrainStateData(TrainStateData trainStateData);
    Task DeleteTrainState(long id);
    Task<ServerMode> GetServerMode();
    Task SetServerMode(ServerMode mode);
    Task SetTimeOffset(int timeOffset);
}

public interface ICommanderTableClientContract
{
    Task ReceiveData(DataToCommanderTable data);
    Task ReceiveServerMode(ServerMode serverMode);
}