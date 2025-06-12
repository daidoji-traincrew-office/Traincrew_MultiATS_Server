using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Services;

public class TrainService(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    OperationNotificationService operationNotificationService,
    ProtectionService protectionService,
    RouteService routeService)
{
    public async Task<ServerToATSData> CreateAtsData(long? clientDriverId, AtsToServerData clientData)
    {
        // �O����H���̍X�V
        List<TrackCircuit> oldTrackCircuitList =
            await trackCircuitService.GetTrackCircuitsByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> oldTrackCircuitDataList =
            oldTrackCircuitList.Select(TrackCircuitService.ToTrackCircuitData).ToList();
        /// <summary>
        /// �V�K�o�^�O����H
        /// </summary>
        List<TrackCircuitData> incrementalTrackCircuitDataList =
            clientData.OnTrackList.Except(oldTrackCircuitDataList).ToList();
        /// <summary>
        /// �ݐ��I���O����H    
        /// </summary>
        List<TrackCircuitData> decrementalTrackCircuitDataList =
            oldTrackCircuitDataList.Except(clientData.OnTrackList).ToList();

        // �O����H���擾���悤�Ƃ���
        var trackCircuitList = await trackCircuitService.GetTrackCircuitsByNames(
            clientData.OnTrackList.Select(tcd => tcd.Name).ToList());
        // Todo: ���������ւ̑Ή����ł�����ȉ��̏����͂���Ȃ�
        // �擾�ł��Ȃ��O����H������ꍇ�A��U�O��̃f�[�^���g��
        if (trackCircuitList.Count != clientData.OnTrackList.Count)
        {
            trackCircuitList = oldTrackCircuitList;
        }



        var ClientTrainNumber = clientData.DiaName;
        // ��ԓo�^���擾
        var TrainStates = new List<TrainState>();
        // �^�Ԃ�������Ԃ̏����擾����
        var TrainState = TrainStates.FirstOrDefault(ts => IsTrainNumberEqual(ts.TrainNumber, ClientTrainNumber));

        ServerToATSData serverData = new ServerToATSData();


        // �����͊��Ə�ɑ��邽�ߋ��ʂŉ��Z����   

        // �ݐ����Ă���O����H��Ŗh�얳�������񂳂�Ă��邩�m�F
        serverData.BougoState = await protectionService.IsProtectionEnabledForTrackCircuits(trackCircuitList);
        // �h�얳���𔭕񂵂Ă���ꍇ��DB�X�V
        if (clientData.BougoState)
        {
            await protectionService.EnableProtectionByTrackCircuits(clientData.DiaName, trackCircuitList);
        }
        else
        {
            await protectionService.DisableProtection(clientData.DiaName);
        }

        // �^�]���m��̕\��
        serverData.OperationNotificationData = await operationNotificationService
            .GetOperationNotificationDataByTrackCircuitIds(trackCircuitList.Select(tc => tc.Id).ToList());

        // �M�������̌v�Z
        // ��肩���肩���f(�����Ȃ���A��Ȃ牺��)
        var lastDiaNumber = clientData.DiaName.Last(char.IsDigit) - '0';
        var isUp = lastDiaNumber % 2 == 0;
        // �Y���O����H�̐M���@��S�擾
        var signalNames = await signalService
            .GetSignalNamesByTrackCircuits(trackCircuitList, isUp);
        // �����v�Z
        // Todo: 1��̐M���@�܂ł͍Œ���v�Z����
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        serverData.NextSignalData = signalIndications.Select(pair => new SignalData
        {
            Name = pair.Key,
            phase = pair.Value
        }).ToList();
        serverData.RouteData = await routeService.GetActiveRoutes();

        // 1.������/����^�Ԃ����o�^
        if (TrainState == null)
        {
            //1-1.�ݐ�������O����H�Ɋ��ɕʉ^�]�m�̗�Ԃ�1�ł��ݐ����Ă���ꍇ�A�����Ƃ��ēo�^�������Ȃ��B

            //1-2.9999��Ԃ̏ꍇ�͗�ԏ���o�^���Ȃ��B

            if (clientData.DiaName == "9999")
            {
                // 9999��Ԃ͗�ԏ���o�^���Ȃ����A�ݐ��͏������ށB     
                await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
                return serverData;
            }
            //1.���S�V�K�o�^
            //���M���ꂽ���Ɋ�Â��ĐV�K�ɏ����������ށB

        }
        else
        {
            // ����^�ԗ�Ԃ��o�^��
            var TrainStateDriverId = TrainState.DriverId;
            // 2.�^�p��/�ʉ^�]�m
            if (TrainStateDriverId != null && TrainStateDriverId != clientDriverId)
            {
                // 2.���O����
                // ���M���Ă����N���C�A���g�ɑ΂����O�������s���A���M���ꂽ���͍ݐ����܂߂Ă��ׂĔj������B  
                serverData.IsOnPreviousTrain = true;

                // �h�얳���̏��́A�^�p����Ԃ̍ݐ��O����H�ƃN���C�A���g�̍ݐ��O����H�����S��v���Ă���Ƃ��̂ݑ��M����B
                // �����ɏ�񂪓o�^����Ă��邽�߁A��L�̋t�̂Ƃ�false�ŏ㏑������B


                return serverData;
            }
            // ���̒n�_�ōݐ�����o�^���Ă悢

            // 3.�^�p�I��
            if (TrainStateDriverId == null)
            {
                // 3.���ύX
                // �����Ŕ������ꂽ���ɂ��āA���M���ꂽ���Ɋ�Â��ď���ύX����B


            }
            // 4.�����Ԃ��o�^��/�^�p��/����^�]�m
            else if (TrainState.TrainNumber == ClientTrainNumber && TrainStateDriverId == clientDriverId)
            {
                // 4.���ύX�Ȃ�
                // ��ԏ��ɂ��Ă͕ύX���Ȃ�
            }
            else
            {
                // �����ɂ͗��Ȃ�
                // �ُ퉞���Ȃǂ�Ԃ��ׂ�
            }
        }

        // �ݐ��O����H�̍X�V
        await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(decrementalTrackCircuitDataList);

        // �ԗ����̓o�^





        return serverData;
    }

    /// <summary>
    /// �^�Ԃ��������ǂ����𔻒肷��
    /// </summary>
    /// <param name="diaName1"></param>
    /// <param name="diaName2"></param>
    /// <returns></returns>
    private bool IsTrainNumberEqual(string diaName1, string diaName2)
    {
        var trainNumber1 = GetTrainNumberFromDiaName(diaName1);
        var trainNumber2 = GetTrainNumberFromDiaName(diaName2);
        return trainNumber1 == trainNumber2;
    }

    /// <summary>
    /// �^�Ԃ����߂�
    /// </summary>
    /// <param name="diaName"></param>
    /// <returns></returns>
    private int GetTrainNumberFromDiaName(string diaName)
    {
        if (diaName == "9999")
        {
            return 400;
        }
        var isTrain = int.TryParse(Regex.Replace(diaName, @"[^0-9]", ""), out var numBody);  // ��Ԗ{�́i���������j
        if (isTrain)
        {
            return numBody / 3000 * 100 + numBody % 100;
        }
        // DiaName�̍Ō�̐������擾
        return 0;
    }
}
