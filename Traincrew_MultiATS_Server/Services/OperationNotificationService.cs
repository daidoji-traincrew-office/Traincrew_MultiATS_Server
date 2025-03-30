using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;

namespace Traincrew_MultiATS_Server.Services;

public class OperationNotificationService(
    IOperationNotificationRepository operationNotificationRepository,
    IDateTimeRepository dateTimeRepository)
{
    public async Task<List<OperationNotificationData>> GetOperationNotificationData()
    {
        var displays = await operationNotificationRepository.GetAllDisplay();
        return displays.Select(display => new OperationNotificationData
        {
            DisplayName = display.Name, 
            Type = display.OperationNotificationState.Type,
            Content = display.OperationNotificationState.Content,
            OperatedAt = display.OperationNotificationState.OperatedAt
        }).ToList();
    }
    
    public async Task<OperationNotificationData?> GetOperationNotificationDataByTrackCircuitIds(
        List<ulong> trackCircuitIds)
    {
        var displays = await operationNotificationRepository.GetDisplayByTrackCircuitIds(trackCircuitIds);
        // Todo: 実装する
        return null;
    }

    public async Task SetOperationNotificationData(OperationNotificationData operationNotificationData)
    {
        var state = new OperationNotificationState
        {
            DisplayName = operationNotificationData.DisplayName,
            Type = operationNotificationData.Type,
            Content = operationNotificationData.Content,
            OperatedAt = dateTimeRepository.GetNow()
        };

        await operationNotificationRepository.SaveState(state);
    }
}