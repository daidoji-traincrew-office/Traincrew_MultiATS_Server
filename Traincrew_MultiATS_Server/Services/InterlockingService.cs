using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 連動装置装置卓
/// </summary>
public class InterlockingService(
        IInterlockingObjectRepository interlockingObjectRepository,
        IDestinationButtonRepository destinationButtonRepository,
        IGeneralRepository generalRepository,
        ILeverRepository leverRepository)
{
    /// <summary>
    /// レバーの物理状態を設定する
    /// </summary>
    /// <param name="leverData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        var lever = await leverRepository.GetLeverByNameWitState(leverData.Name); 
        if (lever == null)
        {
            throw new ArgumentException("Invalid lever name");
        }
        lever.LeverState.IsReversed = leverData.State;
        await generalRepository.Save(lever);
    }

    /// <summary>
    /// 着点ボタンの物理状態を設定する
    /// </summary>
    /// <param name="buttonData"></param>
    /// <returns></returns>     
    /// <exception cref="ArgumentException"></exception>
    public async Task SetDestinationButtonState(DestinationButtonState buttonData)
    {
        var buttonObject = await destinationButtonRepository.GetButtonByName(buttonData.Name);
        if (buttonObject == null)
        {
            throw new ArgumentException("Invalid button name");
        }
        buttonObject.DestinationButtonState.IsRaised = buttonData.IsRaised;
        await generalRepository.Save(buttonObject.DestinationButtonState);
    }

    public async Task<List<InterlockingObject>> GetInterlockingObjects()
    {
        return await interlockingObjectRepository.GetAllWithState();
    }

    public async Task<List<InterlockingObject>> GetObjectsByStationNames(List<string> stationNames)
    {
        return await interlockingObjectRepository.GetObjectsByStationNamesWithState(stationNames);
    }

    public static InterlockingLeverData ToLeverData(Lever lever)
    {
        return new InterlockingLeverData
        {
            Name = lever.Name,
            State = lever.LeverState.IsReversed
        };
    }

    public async Task<List<DestinationButton>> GetDestinationButtons()
    {
        var buttons = await destinationButtonRepository.GetAllButtons();
        return buttons.Values.ToList();
    }

    public async Task<List<DestinationButton>> GetDestinationButtonsByStationNames(List<string> stationNames)
    {
        var buttons = await destinationButtonRepository.GetButtonsByStationNames(stationNames);
        return buttons.Where(button => button != null).Select(button => button!).ToList();
    }
}
