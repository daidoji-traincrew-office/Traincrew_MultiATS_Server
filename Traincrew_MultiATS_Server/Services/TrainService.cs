using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainCar;

namespace Traincrew_MultiATS_Server.Services;

public partial class TrainService(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    OperationNotificationService operationNotificationService,
    ProtectionService protectionService,
    RouteService routeService,
    ITrainRepository trainRepository,
    ITrainCarRepository trainCarRepository
)
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexIsDigits();
    
    public async Task<ServerToATSData> CreateAtsData(ulong clientDriverId, AtsToServerData clientData)
    {
        // 軌道回路情報の更新
        List<TrackCircuit> oldTrackCircuitList =
            await trackCircuitService.GetTrackCircuitsByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> oldTrackCircuitDataList =
            oldTrackCircuitList.Select(TrackCircuitService.ToTrackCircuitData).ToList();
        // 新規登録軌道回路
        List<TrackCircuitData> incrementalTrackCircuitDataList =
            clientData.OnTrackList.Except(oldTrackCircuitDataList).ToList();
        // 在線終了軌道回路    
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

        var clientTrainNumber = clientData.DiaName;
        var clientDiaNumber = GetDiaNumberFromTrainNumber(clientTrainNumber);
        // 運番が同じ列車の情報を取得する
        var trainState = await GetTrainStatesByDiaNumber(clientDiaNumber);

        // ☆情報は割と常に送るため共通で演算する   
        var serverData = new ServerToATSData
        {
            // 在線している軌道回路上で防護無線が発報されているか確認
            BougoState = await protectionService.IsProtectionEnabledForTrackCircuits(trackCircuitList)
        };


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
        if (trainState == null)
        {
            //1-1.在線させる軌道回路に既に別運転士の列番が1つでも在線している場合、早着として登録処理しない。
            var otherTrainStates = await GetTrainStatesByTrackCircuits(trackCircuitList);
            if(otherTrainStates.Any(otherTrainState => otherTrainState.DriverId != null && otherTrainState.DriverId != clientDriverId))
            {
                // 早着として登録しない
                // 早着の列車情報は登録しない
                return serverData;
            }
            //1-2.9999列番の場合は列車情報を登録しない。
            if (clientData.DiaName == "9999")
            {
                await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
                return serverData;
            }
            //1.完全新規登録
            trainState = await CreateTrainState(clientData, clientDriverId);
        }
        else
        {
            // 同一運番列車が登録済
            var TrainStateDriverId = trainState.DriverId;
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
            else if (trainState.TrainNumber == clientTrainNumber && TrainStateDriverId == clientDriverId)
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
        await UpdateTrainCarStates(trainState.Id, clientData.CarStates);
        return serverData;
    }
    
    private async Task<TrainState?> GetTrainStatesByDiaNumber(int diaNumber)
    {
        // 列車情報を取得
        return await trainRepository.GetByDiaNumber(diaNumber);
    }

    // 軌道回路に対する列車の取得
    private async Task<List<TrainState>> GetTrainStatesByTrackCircuits(List<TrackCircuit> trackCircuits)
    {
        // 軌道回路の列車番号を取得し、重複を排除
        var trainNumbers = trackCircuits
            .Select(tc => tc.TrackCircuitState.TrainNumber)
            .Where(trainNumber => !string.IsNullOrEmpty(trainNumber))
            .ToHashSet();
        // 列車番号から列車情報を取得
        return await trainRepository.GetByTrainNumbers(trainNumbers);
    }

    // TrainState新規書き込み
    private async Task<TrainState> CreateTrainState(AtsToServerData clientData, ulong driverId)
    {
        var trainState = new TrainState
        {
            TrainNumber = clientData.DiaName,
            DiaNumber = GetDiaNumberFromTrainNumber(clientData.DiaName),
            FromStationId = string.Empty, // 必要に応じて設定
            ToStationId = string.Empty,   // 必要に応じて設定
            Delay = 0,                    // 必要に応じて設定
            DriverId = driverId
        };
        // 保存処理
        await trainRepository.Create(trainState);
        return trainState;
    }
    
    /// <summary>
    /// TrainState更新
    /// </summary>
    private async Task UpdateTrainState(TrainState trainState)
    {
        // 保存処理（ITrainRepositoryにUpdateTrainメソッドが必要）
        // await trainRepository.UpdateTrain(trainState);
    }

    /// <summary>
    /// TrainCarState更新
    /// </summary> 
    private async Task UpdateTrainCarStates(long trainStateId, List<CarState> carStates)
    {
        var trainCarStates = carStates.Select(cs => new TrainCarState
        {
            CarModel = cs.CarModel,
            HasPantograph = cs.HasPantograph,
            HasDriverCab = cs.HasDriverCab,
            HasConductorCab = cs.HasConductorCab,
            HasMotor = cs.HasMotor,
            DoorClose = cs.DoorClose,
            BcPress = cs.BC_Press,
            Ampare = cs.Ampare,
        }).ToList();
        await trainCarRepository.UpdateAll(trainStateId, trainCarStates);
    }
   
    /// <summary>
    /// TrainState並びにTrainCarStateの削除
    /// </summary>
    private async Task DeleteTrainState(string trainNumber)
    {
        // Todo: 列車情報の削除処理(ITrainRepositoryにDeleteTrainメソッドが必要)
    }


    /// <summary>
    /// 運番が同じかどうかを判定する
    /// </summary>
    /// <param name="trainNumber1">列番1</param>
    /// <param name="trainNumber2">列番2</param>
    /// <returns></returns>
    private static bool IsDiaNumberEqual(string trainNumber1, string trainNumber2)
    {
        var diaNumber1 = GetDiaNumberFromTrainNumber(trainNumber1);
        var diaNumber2 = GetDiaNumberFromTrainNumber(trainNumber2);
        return diaNumber1 == diaNumber2;
    }

    /// <summary>
    /// 列車番号から運番を求める
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    /// <returns></returns>
    private static int GetDiaNumberFromTrainNumber(string trainNumber)
    {
        if (trainNumber == "9999")
        {
            return 400;
        }
        // 列番本体（数字部分）
        var isTrain = int.TryParse(
            RegexIsDigits().Match(trainNumber).Value,
            out var numBody);  
        if (isTrain)
        {
            return numBody / 3000 * 100 + numBody % 100;
        }
        // DiaNameの最後の数字を取得
        return 0;
    }
}
