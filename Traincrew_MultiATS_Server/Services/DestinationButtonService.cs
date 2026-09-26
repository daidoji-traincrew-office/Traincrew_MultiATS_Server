using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.General;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 着点ボタンに関するサービスクラス
/// </summary>
/// <remarks>
/// このサービスは連動装置のmutexを取得しない。
/// 状態を書き換えるメソッドは、呼び出し元で
/// <c>IMutexRepository.AcquireAsync(nameof(InterlockingService))</c> を取得済みであること。
/// (MutexRepositoryは再入不可のため、このサービス側で取り直すとデッドロックする)
/// </remarks>
public interface IDestinationButtonService
{
    /// <summary>
    /// すべての着点ボタンとその状態を取得する
    /// </summary>
    Task<List<DestinationButtonData>> GetAllButtonData();

    /// <summary>
    /// 着点ボタンの物理状態を設定する
    /// </summary>
    /// <exception cref="ArgumentException">名前に対応する着点ボタンが存在しない場合</exception>
    Task<DestinationButtonData> SetState(DestinationButtonData buttonData);

    /// <summary>
    /// 圧下してから1秒超過した着点ボタンを元に戻す
    /// </summary>
    Task ResetRaisedButtonsAsync();
}

/// <inheritdoc cref="IDestinationButtonService"/>
public class DestinationButtonService(
    IDestinationButtonRepository destinationButtonRepository,
    IGeneralRepository generalRepository,
    IDateTimeRepository dateTimeRepository,
    ILogger<DestinationButtonService> logger) : IDestinationButtonService
{
    public async Task<List<DestinationButtonData>> GetAllButtonData()
    {
        var buttons = await destinationButtonRepository.GetAllWithState();
        return buttons
            .Select(button => ToDestinationButtonData(button.DestinationButtonState))
            .ToList();
    }

    public async Task<DestinationButtonData> SetState(DestinationButtonData buttonData)
    {
        var buttonObject = await destinationButtonRepository.GetButtonByName(buttonData.Name);
        if (buttonObject == null)
        {
            throw new ArgumentException("Invalid button name");
        }

        var oldIsRaised = buttonObject.DestinationButtonState.IsRaised;
        buttonObject.DestinationButtonState.OperatedAt = dateTimeRepository.GetNow();
        buttonObject.DestinationButtonState.IsRaised = buttonData.IsRaised;

        if (oldIsRaised != buttonData.IsRaised)
        {
            logger.LogDebug("[{LogType}] 名前: {ButtonName} 状態: {OldState} -> {NewState}",
                "着点操作", buttonObject.DestinationButtonState.Name, oldIsRaised, buttonData.IsRaised);
        }

        await generalRepository.Save(buttonObject.DestinationButtonState);
        return ToDestinationButtonData(buttonObject.DestinationButtonState);
    }

    public async Task ResetRaisedButtonsAsync()
    {
        var now = dateTimeRepository.GetNow();
        await destinationButtonRepository.UpdateRaisedButtonsAsync(now);
    }

    public static DestinationButtonData ToDestinationButtonData(DestinationButtonState buttonState)
    {
        return new()
        {
            Name = buttonState.Name,
            IsRaised = buttonState.IsRaised,
            OperatedAt = buttonState.OperatedAt
        };
    }
}
