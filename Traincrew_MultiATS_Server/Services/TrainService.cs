using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainCar;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;

namespace Traincrew_MultiATS_Server.Services;

public partial class TrainService(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    OperationNotificationService operationNotificationService,
    ProtectionService protectionService,
    RouteService routeService,
    ITrainRepository trainRepository,
    ITrainCarRepository trainCarRepository,
    ITrainDiagramRepository trainDiagramRepository,
    INextSignalRepository nextSignalRepository
)
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexIsDigits();

    public async Task<ServerToATSData> CreateAtsData(ulong clientDriverId, AtsToServerData clientData)
    {
        // 軌道回路情報の更新
        var oldTrackCircuitList = await trackCircuitService.GetTrackCircuitsByTrainNumber(clientData.DiaName);
        var oldTrackCircuitDataList = oldTrackCircuitList.Select(TrackCircuitService.ToTrackCircuitData).ToList();
        // 新規登録軌道回路
        var incrementalTrackCircuitDataList = clientData.OnTrackList.Except(oldTrackCircuitDataList).ToList();
        // 在線終了軌道回路    
        var decrementalTrackCircuitDataList = oldTrackCircuitDataList.Except(clientData.OnTrackList).ToList();

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
        var closeSignalName = await signalService
            .GetSignalNamesByTrackCircuits(trackCircuitList, isUp);
        // 各信号機の１つ先の信号機も取得
        var nextSignalName = await nextSignalRepository
            .GetByNamesAndDepth(closeSignalName, 1);
        // 取得した信号機を結合
        var signalName = closeSignalName
            .Concat(nextSignalName.Select(x => x.TargetSignalName))
            .Distinct()
            .ToList();
        // 現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalName);
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
            if (otherTrainStates.Any(otherTrainState =>
                    otherTrainState.DriverId != null && otherTrainState.DriverId != clientDriverId))
            {
                // QA: 早着の場合クライアントに対してレスポンス返さなくても良い？
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
            var trainStateDriverId = trainState.DriverId;
            // 2.運用中/別運転士
            if (trainStateDriverId != null && trainStateDriverId != clientDriverId)
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
            if (trainStateDriverId == null)
            {
                // 3.情報変更
                // 検索で発見された情報について、送信された情報に基づいて情報を変更する。
                trainState.TrainNumber = clientData.DiaName;
                trainState.DiaNumber = GetDiaNumberFromTrainNumber(clientData.DiaName);
                trainState.DriverId = clientDriverId;
                await UpdateTrainState(trainState);
            }
            // 4.同一列番が登録済/運用中/同一運転士
            else if (trainState.TrainNumber == clientTrainNumber && trainStateDriverId == clientDriverId)
            {
                // 4.情報変更なし
                // 列車情報については変更しない
            }
            else
            {
                // ここには来ない
                // 異常応答などを返すべき
                throw new InvalidOperationException("Unreachable code: TrainState mismatch.");
            }
        }

        // 在線軌道回路の更新
        await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(decrementalTrackCircuitDataList);

        // 車両情報の登録
        await UpdateTrainCarStates(trainState.Id, clientData.CarStates);
        return serverData;
    }

    public async Task DriverGetsOff(ulong clientDriverId, string trainNumber)
    {
        var clientDiaNumber = GetDiaNumberFromTrainNumber(trainNumber);
        var trainState = await GetTrainStatesByDiaNumber(clientDiaNumber);
        if (trainState == null || clientDriverId != trainState.DriverId)
        {
            return;
        }

        trainState.DriverId = null;
        await UpdateTrainState(trainState);
    }

    public async Task<Dictionary<string, TrainInfo>> GetTrainInfoByTrainNumber()
    {
        // 列車情報を取得
        var trainStates = await trainRepository.GetAll();
        // 車両情報を取得
        var trainCarStates = await trainCarRepository.GetAll();
        // 車両情報を列車状態IDでグループ化
        var trainCarStatesByTrainStateId = trainCarStates
            .GroupBy(carState => carState.TrainStateId)
            .ToDictionary(group => group.Key, group => group.ToList());
        // 列車のダイアグラムを取得
        var trainDiagrams = await trainDiagramRepository.GetByTrainNumbers(
            trainStates
                .Select(carState => carState.TrainNumber)
                .ToHashSet());
        // 列車番号ごとのダイアグラム
        var trainDiagramsByTrainNumber = trainDiagrams.ToDictionary(td => td.TrainNumber, td => td);

        // 列車情報と車両情報を結合
        return trainStates
            .ToDictionary(
                trainState => trainState.TrainNumber,
                trainState =>
                {
                    var carStates = trainCarStatesByTrainStateId
                        .GetValueOrDefault(trainState.Id, [])
                        .Select(ToCarState)
                        .ToList();
                    var trainDiagram = trainDiagramsByTrainNumber.GetValueOrDefault(trainState.TrainNumber);
                    return ToTrainInfo(trainState, carStates, trainDiagram);
                });
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
        var trainDiagram = await trainDiagramRepository.GetByTrainNumber(clientData.DiaName);

        var trainState = new TrainState
        {
            TrainNumber = clientData.DiaName,
            DiaNumber = GetDiaNumberFromTrainNumber(clientData.DiaName),
            FromStationId = trainDiagram?.FromStationId ?? "TH00",
            ToStationId = trainDiagram?.ToStationId ?? "TH00",
            Delay = 0, // 必要に応じて設定
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
        await trainRepository.Update(trainState);
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
    public async Task DeleteTrainState(string trainNumber)
    {
        await trainCarRepository.DeleteByTrainNumber(trainNumber);
        await trainRepository.DeleteByTrainNumber(trainNumber);
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
        if (!isTrain)
        {
            // 列番が数字でない場合は運番を0とする
            return 0;
        }

        // 偶数に切り捨ててから計算
        var evenNumBody = numBody % 2 == 0 ? numBody : numBody - 1;
        return evenNumBody / 3000 * 100 + evenNumBody % 100;
    }

    private static TrainInfo ToTrainInfo(TrainState trainState, List<CarState> carStates, TrainDiagram? trainDiagram)
    {
        return new()
        {
            Name = trainState.TrainNumber,
            CarStates = carStates,
            TrainClass = (int)(trainDiagram?.TrainType.Id ?? 0),
            FromStation = trainDiagram?.FromStationId ?? "TH00",
            DestinationStation = trainDiagram?.ToStationId ?? "TH00",
            Delay = trainState.Delay
        };
    }

    private static CarState ToCarState(TrainCarState trainCarState)
    {
        return new()
        {
            CarModel = trainCarState.CarModel,
            HasPantograph = trainCarState.HasPantograph,
            HasDriverCab = trainCarState.HasDriverCab,
            HasConductorCab = trainCarState.HasConductorCab,
            HasMotor = trainCarState.HasMotor,
            DoorClose = trainCarState.DoorClose,
            BC_Press = (float)trainCarState.BcPress,
            Ampare = (float)trainCarState.Ampare
        };
    }
}