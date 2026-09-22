using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.General;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 開放てこに関するサービスクラス
/// </summary>
/// <remarks>
/// このサービスは連動装置のmutexを取得しない。
/// 状態を書き換えるメソッドは、呼び出し元で
/// <c>IMutexRepository.AcquireAsync(nameof(InterlockingService))</c> を取得済みであること。
/// (MutexRepositoryは再入不可のため、このサービス側で取り直すとデッドロックする)
/// </remarks>
public interface IDirectionSelfControlLeverService
{
    /// <summary>
    /// すべての開放てことその状態を取得する
    /// </summary>
    Task<List<DirectionSelfControlLever>> GetAllWithState();

    /// <summary>
    /// 開放てこの物理状態を設定する
    /// </summary>
    /// <param name="keyLeverData">鍵てこの操作内容</param>
    /// <param name="memberId">DiscordのメンバーID</param>
    /// <returns>
    /// 設定後の状態。名前に対応する開放てこが存在しない場合は null
    /// (呼び出し元が他種別の鍵てこへフォールバックできるようにするため)
    /// </returns>
    Task<InterlockingKeyLeverData?> TrySetKeyLeverData(InterlockingKeyLeverData keyLeverData, ulong? memberId);
}

/// <inheritdoc cref="IDirectionSelfControlLeverService"/>
public class DirectionSelfControlLeverService(
    IDirectionSelfControlLeverRepository directionSelfControlLeverRepository,
    IGeneralRepository generalRepository,
    IDiscordService discordService) : IDirectionSelfControlLeverService
{
    public Task<List<DirectionSelfControlLever>> GetAllWithState()
    {
        return directionSelfControlLeverRepository.GetAllWithState();
    }

    public async Task<InterlockingKeyLeverData?> TrySetKeyLeverData(
        InterlockingKeyLeverData keyLeverData, ulong? memberId)
    {
        var directionKeyLever =
            await directionSelfControlLeverRepository.GetDirectionSelfControlLeverByNameWithState(keyLeverData.Name);
        if (directionKeyLever?.DirectionSelfControlLeverState == null)
        {
            return null;
        }

        // 更新後の値定義
        var isInsertedKey = directionKeyLever.DirectionSelfControlLeverState.IsInsertedKey;
        var isReversed = directionKeyLever.DirectionSelfControlLeverState.IsReversed;

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
        if (isInsertedKey != directionKeyLever.DirectionSelfControlLeverState.IsInsertedKey ||
            isReversed != directionKeyLever.DirectionSelfControlLeverState.IsReversed)
        {
            directionKeyLever.DirectionSelfControlLeverState.IsInsertedKey = isInsertedKey;
            directionKeyLever.DirectionSelfControlLeverState.IsReversed = isReversed;
            await generalRepository.Save(directionKeyLever);
        }

        return new()
        {
            Name = directionKeyLever.Name,
            State = isReversed == NR.Reversed ? LNR.Right : LNR.Normal,
            IsKeyInserted = isInsertedKey
        };
    }

    public static InterlockingKeyLeverData ToKeyLeverData(DirectionSelfControlLever lever)
    {
        if (lever.DirectionSelfControlLeverState == null)
        {
            throw new ArgumentException("Invalid lever state");
        }

        return new()
        {
            Name = lever.Name,
            State = lever.DirectionSelfControlLeverState.IsReversed == NR.Reversed ? LNR.Right : LNR.Normal,
            IsKeyInserted = lever.DirectionSelfControlLeverState.IsInsertedKey
        };
    }
}
