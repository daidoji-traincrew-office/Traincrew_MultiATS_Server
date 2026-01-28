using System.Data;
using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainCar;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;
using Traincrew_MultiATS_Server.Repositories.TrainSignalState;
using Traincrew_MultiATS_Server.Repositories.Transaction;

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
    ITransactionRepository transactionRepository,
    BannedUserService bannedUserService,
    IGeneralRepository generalRepository,
    ServerService serverService,
    INextSignalRepository nextSignalRepository,
    ITrainSignalStateRepository trainSignalStateRepository,
    ITrackCircuitDepartmentTimeRepository trackCircuitDepartmentTimeRepository,
    IDateTimeRepository dateTimeRepository,
    ILogger<TrainService> logger
)
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexIsDigits();

    /// <summary>
    /// 営業日の開始時刻。鉄道の営業日は通常4:00から開始される。
    /// </summary>
    private static readonly TimeSpan ServiceDayStartTime = TimeSpan.FromHours(4);

    public async Task<ServerToATSData> CreateAtsData(ulong clientDriverId, AtsToServerData clientData)
    {
        var serverMode = await serverService.GetServerModeAsync();
        // 定時処理が停止している場合、その旨だけ返す
        if (serverMode == ServerMode.Off)
        {
            return new()
            {
                StatusFlags = ServerStatusFlags.IsServerStopped
            };
        }

        // 接続拒否チェック
        var isBanned = await bannedUserService.IsUserBannedAsync(clientDriverId);
        if (isBanned)
        {
            return new()
            {
                StatusFlags = ServerStatusFlags.IsDisconnected
            };
        }

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
            incrementalTrackCircuitDataList.Clear();
            decrementalTrackCircuitDataList.Clear();
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

        // トランザクション開始
        await using var transaction = await transactionRepository.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        // 運番が同じ列車の情報を取得する
        var trainState = await RegisterOrUpdateTrainState(
            clientDriverId, clientData, trackCircuitList, incrementalTrackCircuitDataList,
            serverData);

        if (trainState == null)
        {
            // 列車情報の更新が不要な場合は、ここで終了
            await transaction.CommitAsync();
            return serverData;
        }

        // 在線軌道回路の更新
        if (incrementalTrackCircuitDataList.Count > 0 || decrementalTrackCircuitDataList.Count > 0)
        {
            logger.LogDebug("[{LogType}] 列車: {trainNumber}, 落下: [{NewTrackCircuits}], 扛上: [{EndTrackCircuits}]",
                "在線更新", clientTrainNumber,
                string.Join(", ", incrementalTrackCircuitDataList.Select(tc => tc.Name)),
                string.Join(", ", decrementalTrackCircuitDataList.Select(tc => tc.Name)));
        }

        await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientTrainNumber);
        await trackCircuitService.ClearTrackCircuitDataList(decrementalTrackCircuitDataList);

        // 車両情報の登録
        await UpdateTrainCarStates(trainState.Id, clientData.CarStates);

        // TrainSignalStateの更新
        if (clientData.VisibleSignalNames is { Count: > 0 })
        {
            await trainSignalStateRepository.UpdateByTrainNumber(clientTrainNumber, clientData.VisibleSignalNames);
        }

        // NextSignalNamesの設定
        serverData.NextSignalNames = await GetNextSignalNames(clientTrainNumber, clientData.VisibleSignalNames);

        await transaction.CommitAsync();

        // 遅延計算
        if (decrementalTrackCircuitDataList.Count > 0 && trainState != null)
        {
            const int diaId = 1; // 現在は固定値、将来的に複数ダイヤ対応
            await CalculateAndUpdateDelays(
                diaId,
                clientTrainNumber,
                clientData.CarStates.Count,
                decrementalTrackCircuitDataList);
        }

        return serverData;
    }

    public async Task DriverGetsOff(ulong clientDriverId, string trainNumber)
    {
        var clientDiaNumber = GetDiaNumberFromTrainNumber(trainNumber);
        var trainStates = await GetTrainStatesByDiaNumber(clientDiaNumber);
        foreach (var trainState in trainStates)
        {
            if (clientDriverId != trainState.DriverId)
            {
                continue;
            }

            trainState.DriverId = null;
            await UpdateTrainState(trainState);
        }
    }

    /// <summary>
    /// スケジューラーから定期的にATSに送信するデータを生成する。
    /// </summary>
    /// <returns>ATS向けデータ</returns>
    public async Task<ServerToATSDataBySchedule> CreateDataBySchedule()
    {
        var timeOffset = await serverService.GetTimeOffsetAsync();
        var routeData = await routeService.GetActiveRoutes();

        return new()
        {
            TimeOffset = timeOffset,
            RouteData = routeData
        };
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
        // Todo: もうすこし機能的凝集レベルでメソッド分解したい
        var clientTrainNumber = clientData.DiaName;
        var clientDiaNumber = GetDiaNumberFromTrainNumber(clientTrainNumber);
        var existingTrainStates = await GetTrainStatesByDiaNumber(clientDiaNumber);
        var existingTrainStateByMe = existingTrainStates.FirstOrDefault(ts => ts.DriverId == clientDriverId);
        var existingTrainStatesByOther = existingTrainStates
            .Where(ts => ts.DriverId != null && ts.DriverId != clientDriverId)
            .ToList();
        // 同一運転士の別運番の列車が居る場合、削除
        var driverOtherTrain = await GetTrainStatesByDriverId(clientDriverId);
        if (driverOtherTrain != null && driverOtherTrain.DiaNumber != clientDiaNumber)
        {
            // 別の列車が在線している場合は削除
            await DeleteTrainState(driverOtherTrain.TrainNumber);
            await trackCircuitService
                .ClearTrackCircuitByTrainNumber(driverOtherTrain.TrainNumber);
            // 軌道回路情報を再取得しておく(列車が1度消えるので)
            trackCircuits = await trackCircuitService
                .GetTrackCircuitsByNames(trackCircuits.Select(tc => tc.Name).ToList());
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
                serverData.StatusFlags |= ServerStatusFlags.IsOnPreviousTrain;
                return null;
            }

            //1-1-2.在線させる軌道回路が、１つでも鎖錠されていた場合、鎖錠として登録処理しない。
            if (trackCircuits.Any(tc => tc.TrackCircuitState.IsLocked))
            {
                // 鎖錠の列車情報は登録しない
                serverData.StatusFlags |= ServerStatusFlags.IsLocked;
                return null;
            }

            //1-3.9999列番の場合は列車情報を登録しない。
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
            // 2.無効フラグが立っていない場合、別運転士で同一運番の列車が在線している場合
            //   無効フラグが立っていた場合、別運転士で同一列番の列車が在線している場合
            if (
                (!clientData.IsTherePreviousTrainIgnore && existingTrainStatesByOther.Count > 0)
                || (clientData.IsTherePreviousTrainIgnore &&
                    existingTrainStatesByOther.Any(ts => ts.TrainNumber == clientTrainNumber))
            )
            {
                // 2.交代前応答
                // 送信してきたクライアントに対し交代前応答を行い、送信された情報は在線情報含めてすべて破棄する。
                serverData.StatusFlags |= ServerStatusFlags.IsTherePreviousTrain;
                return null;
            }

            //2. 在線させる軌道回路に既に別運転士の列番が1つでも在線している場合、早着として登録処理しない。
            var trainStatesOnTrackCircuits = await GetTrainStatesByTrackCircuits(trackCircuits);
            if (trainStatesOnTrackCircuits.Any(otherTrainState =>
                    otherTrainState.DriverId != null && otherTrainState.DriverId != clientDriverId))
            {
                // 早着の列車情報は登録しない
                serverData.StatusFlags |= ServerStatusFlags.IsOnPreviousTrain;
                return null;
            }

            //2-1. 在線させる軌道回路が、１つでも鎖錠されていた場合、鎖錠として登録処理しない。
            if (trackCircuits.Any(tc => tc.TrackCircuitState.IsLocked))
            {
                // 鎖錠の列車情報は登録しない
                serverData.StatusFlags |= ServerStatusFlags.IsLocked;
                return null;
            }

            // 該当軌道回路すべてを見た時に、２列車以上の在線があった場合
            if (trainStatesOnTrackCircuits.Count >= 2 ||
                trackCircuits.Select(tc => tc.TrackCircuitState.TrainNumber)
                    .Distinct()
                    .Count(trainNumber => !string.IsNullOrEmpty(trainNumber)) >= 2)
            {
                // 早着の列車情報は登録しない
                serverData.StatusFlags |= ServerStatusFlags.IsOnPreviousTrain;
                return null;
            }

            // 交代先の列車を探す
            // 1. 同一列番の列車が存在せず、在線軌道回路に他の列車が在線している場合、それに乗り換える(メインケース、折返し変更もここ)
            existingTrainStateByMe = trainStatesOnTrackCircuits.FirstOrDefault(ts => ts.DriverId == null);
            if (existingTrainStateByMe != null)
            {
                // 3.列車情報を更新
                existingTrainStateByMe.TrainNumber = clientTrainNumber;
                existingTrainStateByMe.DiaNumber = clientDiaNumber;
                existingTrainStateByMe.DriverId = clientDriverId;
                await UpdateTrainState(existingTrainStateByMe);
                return existingTrainStateByMe;
            }

            existingTrainStateByMe =
                existingTrainStates.FirstOrDefault(ts => ts.DiaNumber == clientDiaNumber && ts.DriverId == null);
            // 2. 同一運番の列車が存在する場合、その列車に乗る(途中駅からの再開etcを想定)
            if (existingTrainStateByMe != null)
            {
                // 同一運番の旧列番で在線を取得しなおす
                var oldTrackCircuitList =
                    await trackCircuitService.GetTrackCircuitsByTrainNumber(existingTrainStateByMe.TrainNumber);
                var oldTrackCircuitNames = oldTrackCircuitList.Select(tc => tc.Name).ToHashSet();
                // ワープのおそれがある場合「ワープ？」を返す
                if (
                    !clientData.IsMaybeWarpIgnore
                    && oldTrackCircuitNames.Count >= 1
                    && trackCircuits.Count >= 1
                    && trackCircuits.Any(tc => !oldTrackCircuitNames.Contains(tc.Name))
                )
                {
                    serverData.StatusFlags |= ServerStatusFlags.IsMaybeWarp;
                    return null;
                }

                // 3.列車情報を更新
                existingTrainStateByMe.TrainNumber = clientTrainNumber;
                existingTrainStateByMe.DiaNumber = clientDiaNumber;
                existingTrainStateByMe.DriverId = clientDriverId;
                await UpdateTrainState(existingTrainStateByMe);
                return existingTrainStateByMe;
            }

            // 3. 在線軌道回路に他の列車が在線していない場合、新規登録
            if (trackCircuits.All(tc => !tc.TrackCircuitState.IsShortCircuit))
            {
                return await CreateTrainState(clientData, clientDriverId);
            }

            // ここには来ないはず
            throw new InvalidOperationException("Unexpected state: No train state found for driver.");
        }
        // 運転士が自分の列車が登録済で、列番を変更した場合

        if (existingTrainStateByMe.TrainNumber != clientTrainNumber)
        {
            // 5.列番だけ書き換える
            existingTrainStateByMe.TrainNumber = clientTrainNumber;
            await UpdateTrainState(existingTrainStateByMe);
        }

        return existingTrainStateByMe;
    }

    public async Task<Dictionary<string, TrainInfo>> GetTrainInfoGroupByTrainNumber()
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

    public async Task<List<TrainStateData>> GetAllTrainState()
    {
        var trainStates = await trainRepository.GetAll();
        return trainStates.Select(ToTrainStateData).ToList();
    }

    public async Task<TrainStateData> UpdateTrainStateData(TrainStateData trainStateData)
    {
        // IDで既存の列車状態を取得
        var existingTrainState = await trainRepository.GetById(trainStateData.Id);
        if (existingTrainState == null)
        {
            throw new InvalidOperationException($"ID{trainStateData.Id} の列車が見つかりませんでした");
        }

        // 更新可能なフィールドを設定
        existingTrainState.TrainNumber = trainStateData.TrainNumber;
        existingTrainState.DiaNumber = trainStateData.DiaNumber;
        existingTrainState.Delay = trainStateData.Delay;
        existingTrainState.DriverId = trainStateData.DriverId;
        existingTrainState.FromStationId = trainStateData.FromStationId;
        existingTrainState.ToStationId = trainStateData.ToStationId;

        // 更新
        await trainRepository.Update(existingTrainState);

        // 更新された列車状態を返す
        return ToTrainStateData(existingTrainState);
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
        await trainSignalStateRepository.DeleteByTrainNumber(trainNumber);
        await trainRepository.DeleteByTrainNumber(trainNumber);
    }

    /// <summary>
    /// 指定されたIDの列車情報を削除する
    /// </summary>
    /// <param name="id">列車状態ID</param>
    public async Task DeleteTrainStateById(long id)
    {
        // 列車状態IDで列車情報を取得
        var trainState = await trainRepository.GetById(id);
        if (trainState == null)
        {
            throw new InvalidOperationException($"ID {id} の列車が見つかりませんでした");
        }

        // まず車両情報を削除する
        await trainCarRepository.DeleteByTrainId(id);

        // 列車情報を取得して削除する
        await generalRepository.Delete(trainState);
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

    private static TrainStateData ToTrainStateData(TrainState trainState)
    {
        return new()
        {
            Id = trainState.Id,
            TrainNumber = trainState.TrainNumber,
            DiaNumber = trainState.DiaNumber,
            FromStationId = trainState.FromStationId,
            ToStationId = trainState.ToStationId,
            Delay = trainState.Delay,
            DriverId = trainState.DriverId
        };
    }

    private static bool IsTrainUpOrDown(string trainNumber)
    {
        // 上りか下りか判断(偶数なら上り、奇数なら下り)
        var lastDiaNumber = trainNumber.Last(char.IsDigit) - '0';
        return lastDiaNumber % 2 == 0;
    }

    /// <summary>
    /// NextSignalNamesを取得
    /// VisibleSignalNamesが空ならTrainSignalStateのものを、そうでないならVisibleSignalNamesのものを使って、
    /// NextSignal.SignalNameが一致しているものをすべて取得し、TargetSignalNameでFlatten.Distinctして返す
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="visibleSignalNames">可視信号機名リスト</param>
    /// <returns>次の信号機名リスト</returns>
    private async Task<List<string>> GetNextSignalNames(string trainNumber, List<string> visibleSignalNames)
    {
        const int maxDepth = 3;
        List<string> signalNames;

        if (visibleSignalNames is { Count: > 0 })
        {
            // VisibleSignalNamesを使用
            signalNames = visibleSignalNames;
        }
        else
        {
            // TrainSignalStateから取得
            signalNames = await trainSignalStateRepository.GetSignalNamesByTrainNumber(trainNumber);
        }

        if (signalNames.Count == 0)
        {
            return [];
        }

        // NextSignal.SignalNameが一致しているものをすべて取得
        var nextSignals = await nextSignalRepository.GetByNamesAndMaxDepthOrderByDepth(signalNames, maxDepth);

        // TargetSignalNameでFlatten.Distinctして返す
        return signalNames
            .Concat(nextSignals.Select(ns => ns.TargetSignalName))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 営業日開始時刻を基準に時刻を正規化する。
    /// 営業日開始時刻より前の時刻（例: 02:00）は、前日の遅い時刻として扱うため86400秒を加算する。
    /// </summary>
    /// <param name="time">正規化する時刻</param>
    /// <returns>営業日開始時刻からの経過秒数</returns>
    private static double NormalizeTimeToServiceDay(TimeSpan time)
    {
        var totalSeconds = time.TotalSeconds;

        // 営業日開始時刻より前なら、前日の遅い時刻として扱う
        if (time < ServiceDayStartTime)
        {
            totalSeconds += 86400; // 24時間を加算
        }

        return totalSeconds;
    }

    /// <summary>
    /// 遅延を計算して更新する
    /// </summary>
    /// <param name="diaId">ダイヤID</param>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="carCount">車両両数</param>
    /// <param name="decrementalTrackCircuitDataList">在線終了軌道回路リスト</param>
    public async Task CalculateAndUpdateDelays(
        int diaId,
        string trainNumber,
        int carCount,
        List<TrackCircuitData> decrementalTrackCircuitDataList)
    {
        // 軌道回路名から軌道回路を取得
        var trackCircuitNames = decrementalTrackCircuitDataList.Select(tc => tc.Name).ToList();
        var trackCircuits = await trackCircuitService.GetTrackCircuitsByNames(trackCircuitNames);

        // 駅軌道回路のみフィルタ（StationIdForDelayが設定されている軌道回路）
        var stationTrackCircuits = trackCircuits.Where(tc => tc.StationIdForDelay != null).ToList();

        // 上り下り判定
        var isUp = IsTrainUpOrDown(trainNumber);

        // 現在時刻
        var currentTime = dateTimeRepository.GetNow().TimeOfDay;

        // 現在のTST時差
        var timeOffset = await serverService.GetTimeOffsetAsync();

        // 各駅軌道回路に対して遅延を計算
        foreach (var trackCircuit in stationTrackCircuits)
        {
            // 時刻表を取得
            var timetable = await trainDiagramRepository.GetTimetableByTrainNumberStationIdAndDiaId(
                diaId, trainNumber, trackCircuit.StationIdForDelay);

            if (timetable?.DepartureTime == null)
            {
                continue;
            }

            // 両数を決定: 到着時刻と出発時刻が同じで始発駅でないなら0（通過扱い）
            var carCountToUse = (timetable.ArrivalTime == timetable.DepartureTime && timetable.Index != 1)
                ? 0
                : carCount;

            // 出発時素を取得
            var departmentTime = await trackCircuitDepartmentTimeRepository
                .GetByTrackCircuitIdAndIsUpAndMaxCarCount(trackCircuit.Id, isUp, carCountToUse);

            var timeElement = 0;
            if (departmentTime == null)
            {
                logger.LogWarning(
                    "出発時素が見つかりませんでした。TrackCircuit: {TrackCircuitId}, IsUp: {IsUp}, CarCount: {CarCount}",
                    trackCircuit.Id, isUp, carCountToUse);
            }
            else
            {
                timeElement = departmentTime.TimeElement;
            }

            // 遅延を計算（営業日境界を考慮）
            var adjustedCurrentTime = TimeSpan.FromSeconds(currentTime.TotalSeconds + timeOffset);

            // 営業日開始時刻（4:00）を基準に正規化
            var currentTimeSeconds = NormalizeTimeToServiceDay(adjustedCurrentTime);
            var departureTimeSeconds = NormalizeTimeToServiceDay(timetable.DepartureTime.Value);

            var delaySeconds = currentTimeSeconds - departureTimeSeconds + timeElement;
            var delayMinutes = (int)Math.Round(delaySeconds / 60.0, MidpointRounding.ToZero);

            // 遅延を更新
            await trainRepository.SetDelayByTrainNumber(trainNumber, delayMinutes);
        }
    }
}
