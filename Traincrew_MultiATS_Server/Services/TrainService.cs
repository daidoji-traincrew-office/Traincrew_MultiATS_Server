using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
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
    ITrainDiagramRepository trainDiagramRepository
)
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexIsDigits();

    public async Task<ServerToATSData> CreateAtsData(ulong clientDriverId, AtsToServerData clientData)
    {
        var clientTrainNumber = clientData.DiaName;
        // 軌道回路情報の更新
        var oldTrackCircuitList = await trackCircuitService.GetTrackCircuitsByTrainNumber(clientTrainNumber);
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

        // ☆情報は割と常に送るため共通で演算する   
        var serverData = new ServerToATSData
        {
            // 在線している軌道回路上で防護無線が発報されているか確認
            BougoState = await protectionService.IsProtectionEnabledForTrackCircuits(trackCircuitList)
        };
        // 防護無線を発報している場合のDB更新
        await protectionService.UpdateBougoState(clientTrainNumber, trackCircuitList, clientData.BougoState);

        // 運転告知器の表示
        serverData.OperationNotificationData = await operationNotificationService
            .GetOperationNotificationDataByTrackCircuitIds(trackCircuitList.Select(tc => tc.Id).ToList());

        // 信号現示の計算
        var isUp = IsTrainUpOrDown(clientTrainNumber);
        serverData.NextSignalData = await signalService.GetSignalIndicationDataByTrackCircuits(trackCircuitList, isUp);
        // 開通進路の情報
        serverData.RouteData = await routeService.GetActiveRoutes();


        // 運番が同じ列車の情報を取得する
        var trainState = await RegisterOrUpdateTrainState(
            clientDriverId, clientData, trackCircuitList, incrementalTrackCircuitDataList, serverData);

        if (trainState == null)
        {
            // 列車情報の更新が不要な場合は、ここで終了
            return serverData;
        }

        // 在線軌道回路の更新
        await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientTrainNumber);
        await trackCircuitService.ClearTrackCircuitDataList(decrementalTrackCircuitDataList);

        // 車両情報の登録
        await UpdateTrainCarStates(trainState.Id, clientData.CarStates);
        return serverData;
    }

    public async Task DriverGetsOff(ulong clientDriverId, string trainNumber)
    {
        var clientDiaNumber = GetDiaNumberFromTrainNumber(trainNumber);
        var trainStates = await GetTrainStatesByDiaNumber(clientDiaNumber);
        if (trainStates == null || trainStates.Count == 0)
        {
            return;
        }

        foreach (var trainState in trainStates)
        {
            if (clientDriverId == trainState.DriverId)
            {
                trainState.DriverId = null;
                await UpdateTrainState(trainState);
            }
        }
    }

    /// <summary>
    /// 列車情報の新規登録または更新を行う。
    /// </summary>
    /// <param name="clientDriverId">運転士ID</param>
    /// <param name="clientData">クライアントからのデータ</param>
    /// <param name="trackCircuits">在線軌道回路リスト</param>
    /// <param name="incrementalTrackCircuitDataList">新規登録する軌道回路データリスト</param>
    /// <param name="serverData">サーバーからクライアントへのデータ</param>
    /// <returns>列車状態。登録しない場合はnull。</returns>
    private async Task<TrainState?> RegisterOrUpdateTrainState(
        ulong clientDriverId,
        AtsToServerData clientData,
        List<TrackCircuit> trackCircuits,
        List<TrackCircuitData> incrementalTrackCircuitDataList,
        ServerToATSData serverData)
    {
        var clientTrainNumber = clientData.DiaName;
        var clientDiaNumber = GetDiaNumberFromTrainNumber(clientTrainNumber);
        var existingTrainStates = await GetTrainStatesByDiaNumber(clientDiaNumber);
        var existingTrainStateByMe = existingTrainStates.FirstOrDefault(ts => ts.DriverId == clientDriverId);
        var existingTrainStateByNone = existingTrainStates.FirstOrDefault(ts => ts.DriverId == null);
        var existingTrainStatesByOther = existingTrainStates
            .Where(ts => ts.DriverId != null && ts.DriverId != clientDriverId)
            .ToList();
        // 同一運転士の別運番の列車が居る場合、削除
        var driverOtherTrain = await trainRepository.GetByDriverId(clientDriverId);
        if (driverOtherTrain != null && driverOtherTrain.DiaNumber != clientDiaNumber)
        {
            // 別の列車が在線している場合は削除
            await DeleteTrainState(driverOtherTrain.TrainNumber);
            // existingTrainStatesには同一運番の列車情報が入るので、ここで削除された列車を気にする必要はない
        }

        // 1.同一列番/同一運番が未登録
        if (existingTrainStates.Count == 0)
        {
            //1-1.在線させる軌道回路に既に別運転士の列番が1つでも在線している場合、早着として登録処理しない。
            var otherTrainStates = await GetTrainStatesByTrackCircuits(trackCircuits);
            if (otherTrainStates.Any(otherTrainState =>
                    otherTrainState.DriverId != null && otherTrainState.DriverId != clientDriverId))
            {
                // 早着の列車情報は登録しない
                serverData.IsOnPreviousTrain = true;
                return null;
            }

            //1-2.9999列番の場合は列車情報を登録しない。
            if (clientData.DiaName == "9999")
            {
                await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientTrainNumber);
                return null;
            }


            //1.完全新規登録
            return await CreateTrainState(clientData, clientDriverId);
        }

        // 同一運番列車が登録済

        // 1-1.運転士が自分な列車が未登録
        if (existingTrainStateByMe == null)
        {
            // 2.無効フラグが立っていない場合、同一運番の列車が在線している場合
            //   無効フラグが立っていた場合、同一列番の列車が在線している場合
            if (
                (!clientData.IsTherePreviousTrainIgnore && existingTrainStatesByOther.Count > 0)
                || (clientData.IsTherePreviousTrainIgnore &&
                    existingTrainStatesByOther.Any(ts => ts.TrainNumber == clientTrainNumber))
            )
            {
                // 2.交代前応答
                // 送信してきたクライアントに対し交代前応答を行い、送信された情報は在線情報含めてすべて破棄する。  
                serverData.IsTherePreviousTrain = true;
                return null;
            }
            
            //2. 在線させる軌道回路に既に別運転士の列番が1つでも在線している場合、早着として登録処理しない。
            var trainStatesOnTrackCircuits = await GetTrainStatesByTrackCircuits(trackCircuits);
            if (trainStatesOnTrackCircuits.Any(otherTrainState =>
                    otherTrainState.DriverId != null && otherTrainState.DriverId != clientDriverId))
            {
                // 早着の列車情報は登録しない
                serverData.IsOnPreviousTrain = true;
                return null;
            }

            // 3 誰も乗っていない列車がいた場合、その列車に乗る
            if (trainStatesOnTrackCircuits.Count > 0 
                && trainStatesOnTrackCircuits.All(ts => trainStatesOnTrackCircuits[0].Id == ts.Id))
            {
                // 3.情報変更
                // 検索で発見された情報について、送信された情報に基づいて情報を変更する。
                existingTrainStateByMe = existingTrainStateByNone;
                existingTrainStateByMe.TrainNumber = clientTrainNumber;
                existingTrainStateByMe.DiaNumber = clientDiaNumber;
                existingTrainStateByMe.DriverId = clientDriverId;
                await UpdateTrainState(existingTrainStateByMe);
            }
            // 在線させる軌道回路に列車がいない場合、新規登録とする 
            else if (trainStatesOnTrackCircuits.Count == 0)
            {
                // 4.新規登録
                existingTrainStateByMe = await CreateTrainState(clientData, clientDriverId);
            }
            // ここには入らないはず？？？
            else
            {
                throw new InvalidOperationException(
                    "Unexpected state: 同一運番の列車を登録しますが、軌道回路の状態がおかしいです");
            }
        }
        // 運転士が自分の列車が登録済で、列番を変更した場合
        else if (existingTrainStateByMe.TrainNumber != clientTrainNumber)
        {
            // 5.列番だけ書き換える
            existingTrainStateByMe.TrainNumber = clientTrainNumber;
            await UpdateTrainState(existingTrainStateByMe);
        }

        return existingTrainStateByMe;
    }

    public async Task<Dictionary<string, TrainInfo>> GetTrainInfoByTrainNumber()
    {
        // 列車情報を取得
        var trainStates = await trainRepository.GetAll();
        // 車両情報を取得
        var trainCarStates = await trainCarRepository.GetAllOrderByTrainStateIdAndIndex();
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


    private async Task<List<TrainState>> GetTrainStatesByDiaNumber(int diaNumber)
    {
        // 列車情報を取得
        return await trainRepository.GetByDiaNumber(diaNumber);
    }

    private async Task<TrainState?> GetTrainStatesByDriverId(ulong driverId)
    {
        // 運転士IDに紐づく列車情報を取得
        return await trainRepository.GetByDriverId(driverId);
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
        var trainDiagram = await trainDiagramRepository.GetByTrainNumber(trainState.TrainNumber);
        // 列車のダイアグラム情報を更新
        trainState.FromStationId = trainDiagram?.FromStationId ?? "TH00";
        trainState.ToStationId = trainDiagram?.ToStationId ?? "TH00";
        // 列車情報を更新
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
        await trackCircuitService.ClearTrackCircuitByTrainNumber(trainNumber);
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
            TrainClass = (int)(trainDiagram?.TrainTypeId ?? 0),
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

    private static bool IsTrainUpOrDown(string trainNumber)
    {
        // 上りか下りか判断(偶数なら上り、奇数なら下り)
        var lastDiaNumber = trainNumber.Last(char.IsDigit) - '0';
        return lastDiaNumber % 2 == 0;
    }
}