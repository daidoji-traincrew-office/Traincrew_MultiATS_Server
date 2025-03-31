using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;

namespace Traincrew_MultiATS_Server.Services;

public class OperationNotificationService(
    IOperationNotificationRepository operationNotificationRepository,
    IGeneralRepository generalRepository,
    IDateTimeRepository dateTimeRepository)
{
    public async Task<List<OperationNotificationData>> GetOperationNotificationData()
    {
        var displays = await operationNotificationRepository.GetAllDisplay();
        return displays.Select(ToOperationNotificationData).ToList();
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
        return ToOperationNotificationData(display);
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

        await generalRepository.Save(state);
    }

    private static OperationNotificationData ToOperationNotificationData(
        OperationNotificationDisplay operationNotificationDisplay)
    {
        return new()
        {
            DisplayName = operationNotificationDisplay.Name,
            Type = operationNotificationDisplay.OperationNotificationState.Type,
            Content = operationNotificationDisplay.OperationNotificationState.Content,
            OperatedAt = operationNotificationDisplay.OperationNotificationState.OperatedAt
        };
    }
}