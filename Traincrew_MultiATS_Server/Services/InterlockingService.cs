using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.General;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// �A�����u���u��
/// </summary>
public class InterlockingService(
        IInterlockingObjectRepository interlockingObjectRepository,
        IDestinationButtonRepository destinationButtonRepository,
        IGeneralRepository generalRepository)
{
    /// <summary>
    /// ���o�[�̕�����Ԃ�ݒ肷��
    /// </summary>
    /// <param name="leverData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        var lever = await interlockingObjectRepository.GetObject(leverData.Name);
        if (lever == null)
        {
            throw new ArgumentException("Invalid lever name");
        }
        lever.PhysicalLeverState.IsRaised = leverData.IsRaised;
        await generalRepository.Save(lever);
    }

    /// <summary>
    /// ���_�{�^���̕�����Ԃ�ݒ肷��
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
}
