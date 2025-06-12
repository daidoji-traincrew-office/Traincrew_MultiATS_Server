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
        // 軌道回路情報の更新
        List<TrackCircuit> oldTrackCircuitList =
            await trackCircuitService.GetTrackCircuitsByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> oldTrackCircuitDataList =
            oldTrackCircuitList.Select(TrackCircuitService.ToTrackCircuitData).ToList();
        /// <summary>
        /// 新規登録軌道回路
        /// </summary>
        List<TrackCircuitData> incrementalTrackCircuitDataList =
            clientData.OnTrackList.Except(oldTrackCircuitDataList).ToList();
        /// <summary>
        /// 在線終了軌道回路    
        /// </summary>
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



        var ClientTrainNumber = clientData.DiaName;
        // 列車登録情報取得
        var TrainStates = new List<TrainState>();
        // 運番が同じ列車の情報を取得する
        var TrainState = TrainStates.FirstOrDefault(ts => IsTrainNumberEqual(ts.TrainNumber, ClientTrainNumber));

        ServerToATSData serverData = new ServerToATSData();


        // ☆情報は割と常に送るため共通で演算する   

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
        // Todo: 1つ先の信号機までは最低限計算する
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        serverData.NextSignalData = signalIndications.Select(pair => new SignalData
        {
            Name = pair.Key,
            phase = pair.Value
        }).ToList();
        serverData.RouteData = await routeService.GetActiveRoutes();

        // 1.同一列番/同一運番が未登録
        if (TrainState == null)
        {
            //1-1.在線させる軌道回路に既に別運転士の列番が1つでも在線している場合、早着として登録処理しない。

            //1-2.9999列番の場合は列車情報を登録しない。

            if (clientData.DiaName == "9999")
            {
                // 9999列番は列車情報を登録しないが、在線は書き込む。     
                await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
                return serverData;
            }
            //1.完全新規登録
            //送信された情報に基づいて新規に情報を書き込む。

        }
        else
        {
            // 同一運番列車が登録済
            var TrainStateDriverId = TrainState.DriverId;
            // 2.運用中/別運転士
            if (TrainStateDriverId != null && TrainStateDriverId != clientDriverId)
            {
                // 2.交代前応答
                // 送信してきたクライアントに対し交代前応答を行い、送信された情報は在線情報含めてすべて破棄する。  
                serverData.IsOnPreviousTrain = true;

                // 防護無線の情報は、運用中列車の在線軌道回路とクライアントの在線軌道回路が完全一致しているときのみ送信する。
                // →既に情報が登録されているため、上記の逆のときfalseで上書きする。


                return serverData;
            }
            // この地点で在線情報を登録してよい

            // 3.運用終了
            if (TrainStateDriverId == null)
            {
                // 3.情報変更
                // 検索で発見された情報について、送信された情報に基づいて情報を変更する。


            }
            // 4.同一列番が登録済/運用中/同一運転士
            else if (TrainState.TrainNumber == ClientTrainNumber && TrainStateDriverId == clientDriverId)
            {
                // 4.情報変更なし
                // 列車情報については変更しない
            }
            else
            {
                // ここには来ない
                // 異常応答などを返すべき
            }
        }

        // 在線軌道回路の更新
        await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(decrementalTrackCircuitDataList);

        // 車両情報の登録





        return serverData;
    }

    /// <summary>
    /// 運番が同じかどうかを判定する
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
    /// 運番を求める
    /// </summary>
    /// <param name="diaName"></param>
    /// <returns></returns>
    private int GetTrainNumberFromDiaName(string diaName)
    {
        if (diaName == "9999")
        {
            return 400;
        }
        var isTrain = int.TryParse(Regex.Replace(diaName, @"[^0-9]", ""), out var numBody);  // 列番本体（数字部分）
        if (isTrain)
        {
            return numBody / 3000 * 100 + numBody % 100;
        }
        // DiaNameの最後の数字を取得
        return 0;
    }
}
