using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;
using Traincrew_MultiATS_Server.Services.Cache;

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
    IDateTimeRepository dateTimeRepository,
    ICacheGate cacheGate) : IOperationNotificationService
{
    static readonly int kaijoTime = 20;

    public async Task<List<OperationNotificationData>> GetOperationNotificationData()
    {
        var states = await GetStatesCachedAsync();
        return states.Values.ToList();
    }

    /// <summary>
    /// 告知器の状態を「告知器名 → 表示内容」の全件スナップショットで取得する。
    /// </summary>
    /// <remarks>
    /// ホットパスは告知器名からPKで1行引くだけだが、全件でも告知器の数(48)しかないため
    /// まとめて1エントリで持つ。operation_notification_state を書くのは司令卓の操作と
    /// 同一プロセス内の OperationNotificationScheduler だけなので、その2箇所で無効化すれば漏れがない。
    ///
    /// キャッシュに載せるのは不変DTOであってEFのエンティティではない。
    /// OperationNotificationState を載せるとナビゲーションプロパティ経由で
    /// 共有インスタンスに状態を載せる事故の的になる。
    /// </remarks>
    private async Task<IReadOnlyDictionary<string, OperationNotificationData>> GetStatesCachedAsync()
    {
        return await cacheGate.GetOrFillAsync<IReadOnlyDictionary<string, OperationNotificationData>>(
            CacheKeys.OperationNotificationStates,
            async () => (await operationNotificationRepository.GetAllStates())
                .ToDictionary(state => state.DisplayName, ToOperationNotificationData, StringComparer.Ordinal));
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

        // 名前からStateを引く
        var states = await GetStatesCachedAsync();
        return states.GetValueOrDefault(displayName);
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
        await cacheGate.InvalidateAsync(CacheKeys.OperationNotificationStates);
    }

    public async Task SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime()
    {
        var now = dateTimeRepository.GetNow();
        var operatedAt = now.AddSeconds(-kaijoTime);
        var updatedCount = await operationNotificationRepository
            .SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(operatedAt);
        if (updatedCount > 0)
        {
            // このメソッドは500ms間隔で走る。実際に「なし」へ戻る行があったときだけ無効化する。
            // 無条件に無効化するとキャッシュが意味を失う。
            await cacheGate.InvalidateAsync(CacheKeys.OperationNotificationStates);
        }
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