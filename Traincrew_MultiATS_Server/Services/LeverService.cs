using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Lever;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// てこに関するサービスクラス
/// </summary>
/// <remarks>
/// このサービスは連動装置のmutexを取得しない。
/// 状態を書き換えるメソッドは、呼び出し元で
/// <c>IMutexRepository.AcquireAsync(nameof(InterlockingService))</c> を取得済みであること。
/// (MutexRepositoryは再入不可のため、このサービス側で取り直すとデッドロックする)
/// </remarks>
public interface ILeverService
{
    /// <summary>
    /// すべてのてことその状態を取得する
    /// </summary>
    Task<List<InterlockingLeverData>> GetAllLeverData();

    /// <summary>
    /// てこの物理状態を設定する
    /// </summary>
    /// <exception cref="ArgumentException">名前に対応するてこが存在しない場合</exception>
    Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData);
}

/// <inheritdoc cref="ILeverService"/>
public class LeverService(
    ILeverRepository leverRepository,
    IGeneralRepository generalRepository,
    ILogger<LeverService> logger) : ILeverService
{
    public async Task<List<InterlockingLeverData>> GetAllLeverData()
    {
        var levers = await leverRepository.GetAllWithState();
        return levers
            .Select(ToLeverData)
            .ToList();
    }

    public async Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        var lever = await leverRepository.GetLeverByNameWithState(leverData.Name);
        if (lever == null)
        {
            throw new ArgumentException("Invalid lever name");
        }

        var oldState = lever.LeverState.IsReversed;
        lever.LeverState.IsReversed = leverData.State;

        if (oldState != leverData.State)
        {
            logger.LogDebug("[{LogType}] 名前: {LeverName} 状態: {OldState} -> {NewState}",
                "てこ操作", lever.Name, oldState, leverData.State);
        }

        await generalRepository.Save(lever);
        return new()
        {
            Name = lever.Name,
            State = lever.LeverState.IsReversed
        };
    }

    public static InterlockingLeverData ToLeverData(Lever lever)
    {
        return new()
        {
            Name = lever.Name,
            State = lever.LeverState.IsReversed
        };
    }
}
