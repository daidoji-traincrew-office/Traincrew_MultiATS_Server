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
        var displays = await operationNotificationRepository
            .GetDisplayByTrackCircuitIds(trackCircuitIds);
        // 運転告知器のない軌道回路があるならnullを返す
        if(displays.Any(d => d == null) || displays.Count != 1)
        {
            return null;
        }
        var display = displays.First();
        if(!display.TrackCircuits.Select(tc => tc.Id).ToHashSet().SetEquals(trackCircuitIds.ToHashSet()))
        {
            // まだホームトラックに入りきってない場合、nullを返す
            return null;
        }
        return new()
        {
            DisplayName = display.Name,
            Type = display.OperationNotificationState.Type,
            Content = display.OperationNotificationState.Content,
            OperatedAt = display.OperationNotificationState.OperatedAt
        };
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