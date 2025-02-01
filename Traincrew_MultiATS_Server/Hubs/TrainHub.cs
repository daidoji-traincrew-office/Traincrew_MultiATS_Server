using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TrainHub(TrackCircuitService trackCircuitService, SignalService signalService) : Hub
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
        await trackCircuitService.SetTrackCircuitDataList(Incremental_TrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(Decremental_TrackCircuitDataList);
        
        // 信号現示の計算
        // 上りか下りか判断(偶数なら上り、奇数なら下り)
        var lastDiaNumber = clientData.DiaName.Last(char.IsDigit) - '0';
        var isUp = lastDiaNumber % 2 == 0;
        // 該当軌道回路の信号機を全取得
        var signalNames = await signalService
            .GetSignalNamesByTrackCircuits(clientData.OnTrackList.Select(tcd => tcd.Name).ToList(), isUp);
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