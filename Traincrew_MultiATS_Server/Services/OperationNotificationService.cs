using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;

namespace Traincrew_MultiATS_Server.Services;

public interface IOperationNotificationService
{
    Task<List<OperationNotificationData>> GetOperationNotificationData();
    Task<OperationNotificationData?> GetOperationNotificationDataByTrackCircuitIds(List<ulong> trackCircuitIds);
    Task SetOperationNotificationData(OperationNotificationData operationNotificationData);
    Task SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime();
}

public class OperationNotificationService(
    IOperationNotificationMasterStore operationNotificationMasterStore,
    IOperationNotificationRepository operationNotificationRepository,
    IGeneralRepository generalRepository,
    IDateTimeRepository dateTimeRepository) : IOperationNotificationService
{
    static readonly int kaijoTime = 20;

    public async Task<List<OperationNotificationData>> GetOperationNotificationData()
    {
        // Stateを全取得
        var states = await operationNotificationRepository.GetAllStates();
        return states
            .Select(ToOperationNotificationData)
            .ToList();
    }

    public async Task<OperationNotificationData?> GetOperationNotificationDataByTrackCircuitIds(
        List<ulong> trackCircuitIds)
    {
        var master = operationNotificationMasterStore.Current;
        // 軌道回路に該当する告知器を探す
        var displayName = master.TryGetDisplayName(trackCircuitIds);
        // なければnullで返す
        if (displayName == null)
        {
            return null;
        }

        // 名前からState取得
        var state = await operationNotificationRepository.GetStateByDisplayName(displayName);
        return state != null
            ? ToOperationNotificationData(state)
            : null;
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

    public async Task SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime()
    {
        var now = dateTimeRepository.GetNow();
        var operatedAt = now.AddSeconds(-kaijoTime);
        await operationNotificationRepository.SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(operatedAt);
    }

    private static OperationNotificationData ToOperationNotificationData(OperationNotificationState state)
    {
        return new()
        {
            DisplayName = state.DisplayName,
            Type = state.Type,
            Content = state.Content,
            OperatedAt = state.OperatedAt
        };
    }
}