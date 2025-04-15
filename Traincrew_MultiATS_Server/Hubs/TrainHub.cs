using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

// 運転士 or 車掌使用可能
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "TrainPolicy"
)]
public class TrainHub(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    OperationNotificationService operationNotificationService,
    ProtectionService protectionService,
    RendoService rendoService) : Hub
{
    public async Task<DataFromServer> SendData_ATS(DataToServer clientData)
    {
        // Todo: TrainServiceにロジックもろとも移行する
        DataFromServer serverData = new DataFromServer();
        // 軌道回路情報の更新
        List<TrackCircuit> oldTrackCircuitList =
            await trackCircuitService.GetTrackCircuitsByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> oldTrackCircuitDataList =
            oldTrackCircuitList.Select(TrackCircuitService.ToTrackCircuitData).ToList();
        List<TrackCircuitData> incrementalTrackCircuitDataList =
            clientData.OnTrackList.Except(oldTrackCircuitDataList).ToList();
        List<TrackCircuitData> decrementalTrackCircuitDataList =
            oldTrackCircuitDataList.Except(clientData.OnTrackList).ToList();

        // 軌道回路を取得しようとする
        var trackCircuitList = await trackCircuitService.GetTrackCircuitsByNames(
            clientData.OnTrackList.Select(tcd => tcd.Name).ToList());
        // Todo: 文字化けへの対応ができたら以下の処理はいらない
        // 取得できない軌道回路がある場合、一旦前回のデータを使う
        if (trackCircuitList.Count != clientData.OnTrackList.Count)
        {
            trackCircuitList = oldTrackCircuitList;
        }

        // 在線している軌道回路上で防護無線が発報されているか確認
        serverData.BougoState = await protectionService.IsProtectionEnabledForTrackCircuits(trackCircuitList);
        // 防護無線を発報している場合のDB更新
        if (clientData.BougoState)
        {
            await protectionService.EnableProtectionByTrackCircuits(clientData.DiaName, trackCircuitList);
        }
        else
        {
            await protectionService.DisableProtection(clientData.DiaName);
        }
        // 在線軌道回路の更新
        await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(decrementalTrackCircuitDataList);

        // 運転告知器の表示
        serverData.OperationNotificationData = await operationNotificationService
            .GetOperationNotificationDataByTrackCircuitIds(trackCircuitList.Select(tc => tc.Id).ToList());

        // 信号現示の計算
        // 上りか下りか判断(偶数なら上り、奇数なら下り)
        var lastDiaNumber = clientData.DiaName.Last(char.IsDigit) - '0';
        var isUp = lastDiaNumber % 2 == 0;
        // 該当軌道回路の信号機を全取得
        var signalNames = await signalService
            .GetSignalNamesByTrackCircuits(trackCircuitList, isUp);
        // 現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        serverData.NextSignalData = signalIndications.Select(pair => new SignalData
        {
            Name = pair.Key,
            phase = pair.Value
        }).ToList();
        serverData.RouteData = await rendoService.GetActiveRoutes();
        return serverData;
    }
}