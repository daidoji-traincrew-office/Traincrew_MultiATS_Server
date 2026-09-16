using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// CTC切り替えてこ(進路集中制御てこ)に関するサービスクラス
/// </summary>
/// <remarks>
/// このサービスは連動装置のmutexを取得しない。
/// 状態を書き換えるメソッドは、呼び出し元で
/// <c>IMutexRepository.AcquireAsync(nameof(InterlockingService))</c> を取得済みであること。
/// (MutexRepositoryは再入不可のため、このサービス側で取り直すとデッドロックする)
/// </remarks>
public interface IRouteCentralControlLeverService
{
    /// <summary>
    /// すべてのCTC切り替えてことその状態を取得する
    /// </summary>
    Task<List<RouteCentralControlLever>> GetAllWithState();

    /// <summary>
    /// CTC切り替えてこの物理状態を設定する
    /// </summary>
    /// <param name="keyLeverData">鍵てこの操作内容</param>
    /// <param name="memberId">DiscordのメンバーID</param>
    /// <returns>
    /// 設定後の状態。名前に対応するCTC切り替えてこが存在しない場合は null
    /// (呼び出し元が他種別の鍵てこへフォールバックできるようにするため)
    /// </returns>
    Task<InterlockingKeyLeverData?> TrySetKeyLeverData(InterlockingKeyLeverData keyLeverData, ulong? memberId);
}

/// <inheritdoc cref="IRouteCentralControlLeverService"/>
public class RouteCentralControlLeverService(
    IRouteCentralControlLeverRepository routeCentralControlLeverRepository,
    IDiscordService discordService) : IRouteCentralControlLeverService
{
    public Task<List<RouteCentralControlLever>> GetAllWithState()
    {
        return routeCentralControlLeverRepository.GetAllWithState();
    }

    public async Task<InterlockingKeyLeverData?> TrySetKeyLeverData(
        InterlockingKeyLeverData keyLeverData, ulong? memberId)
    {
        var routeCentralControlLever =
            await routeCentralControlLeverRepository.GetByNameWithState(keyLeverData.Name);
        if (routeCentralControlLever?.RouteCentralControlLeverState == null)
        {
            return null;
        }

        // 更新後の値定義
        var isInsertedKey = routeCentralControlLever.RouteCentralControlLeverState.IsInsertedKey;
        var isReversed = routeCentralControlLever.RouteCentralControlLeverState.IsReversed;

        // 鍵を刺せるか確認
        var role = await discordService.GetRoleByMemberId(memberId);
        // 鍵を刺せるなら、鍵を処理する
        if (role.IsAdministrator)
        {
            isInsertedKey = keyLeverData.IsKeyInserted;
        }

        // 鍵が刺さっている場合、回す処理をする
        if (isInsertedKey)
        {
            isReversed = keyLeverData.State == LNR.Right ? NR.Reversed : NR.Normal;
        }

        // 変化があれば、更新する
        if (isInsertedKey != routeCentralControlLever.RouteCentralControlLeverState.IsInsertedKey ||
            isReversed != routeCentralControlLever.RouteCentralControlLeverState.IsReversed)
        {
            routeCentralControlLever.RouteCentralControlLeverState.IsInsertedKey = isInsertedKey;
            routeCentralControlLever.RouteCentralControlLeverState.IsReversed = isReversed;
            // route_central_control_lever_stateは連動サーバーがis_center_controlledを書くため、列限定更新にすること
            await routeCentralControlLeverRepository.SetIsInsertedKeyAndIsReversedById(
                routeCentralControlLever.Id, isInsertedKey, isReversed);
        }

        return new()
        {
            Name = routeCentralControlLever.Name,
            State = isReversed == NR.Reversed ? LNR.Right : LNR.Left,
            IsKeyInserted = isInsertedKey
        };
    }

    public static InterlockingKeyLeverData ToKeyLeverData(RouteCentralControlLever lever)
    {
        if (lever.RouteCentralControlLeverState == null)
        {
            throw new ArgumentException("Invalid lever state");
        }

        return new()
        {
            Name = lever.Name,
            State = lever.RouteCentralControlLeverState.IsReversed == NR.Reversed ? LNR.Right : LNR.Left,
            IsKeyInserted = lever.RouteCentralControlLeverState.IsInsertedKey
        };
    }

    /// <summary>
    /// CTC切り替えてこから駅扱切換表示灯(CHRNK/CHRRK)の状態を作る
    /// </summary>
    public static IEnumerable<KeyValuePair<string, bool>> ToChrLamps(RouteCentralControlLever lever)
    {
        var stationId = lever.StationId;
        return
        [
            new($"{stationId}_CHRNK", lever.RouteCentralControlLeverState is { IsCenterControlled: false }),
            new($"{stationId}_CHRRK", lever.RouteCentralControlLeverState is { IsCenterControlled: true })
        ];
    }
}
