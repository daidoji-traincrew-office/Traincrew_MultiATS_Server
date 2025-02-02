using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TrainHub(TrackCircuitService trackCircuitService, SignalService signalService, ProtectionService protectionService) : Hub
{
    public async Task<DataFromServer> SendData_ATS(DataToServer clientData)
    {
        // Todo: TrainServiceにロジックもろとも移行する
        DataFromServer serverData = new DataFromServer();
        // 軌道回路情報の更新
        List<TrackCircuitData> old_TrackCircuitDataList =
            await trackCircuitService.GetTrackCircuitDataListByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> Incremental_TrackCircuitDataList =
            clientData.OnTrackList.Except(old_TrackCircuitDataList).ToList();
        List<TrackCircuitData> Decremental_TrackCircuitDataList =
            old_TrackCircuitDataList.Except(clientData.OnTrackList).ToList();
        List<int> bougo_zone = await trackCircuitService.GetBougoZoneByTrackCircuitDataList(clientData.OnTrackList);
        bool BougoState = await protectionService.GetProtectionZoneStateByBougoZone(bougo_zone);
        if (BougoState == true)
        {
            serverData.BougoState = true;
        }
        if (clientData.BougoState == true)
        {
            foreach (var item in bougo_zone)
            {
                await protectionService.EnableProtection(clientData.DiaName, item);
            }
        }
        if(clientData.BougoState == false)
        {
            await protectionService.DisableProtection(clientData.DiaName);
        }
        if(clientData.BougoState == false)
        await trackCircuitService.SetTrackCircuitDataList(Incremental_TrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(Decremental_TrackCircuitDataList);
        
        // 信号現示の計算
        // Todo: 文字化けへの対応ができたらこの処理はいらない
        // 軌道回路を取得しようとする
        var trackCircuitDataList = await trackCircuitService.GetTrackCircuitDataListByNames(
            clientData.OnTrackList.Select(tcd => tcd.Name).ToList());
        // 取得できない軌道回路がある場合、一旦前回のデータを使う
        if (trackCircuitDataList.Count != clientData.OnTrackList.Count)
        {
            trackCircuitDataList = old_TrackCircuitDataList;
        }
        
        // 上りか下りか判断(偶数なら上り、奇数なら下り)
        var lastDiaNumber = clientData.DiaName.Last(char.IsDigit) - '0';
        var isUp = lastDiaNumber % 2 == 0;
        // 該当軌道回路の信号機を全取得
        var signalNames = await signalService
            .GetSignalNamesByTrackCircuits(trackCircuitDataList.Select(tcd => tcd.Name).ToList(), isUp);
        // 現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        serverData.NextSignalData = signalIndications.Select(pair => new SignalData
        {
           Name = pair.Key,
           phase = pair.Value 
        }).ToList();
        return serverData;
    }
}