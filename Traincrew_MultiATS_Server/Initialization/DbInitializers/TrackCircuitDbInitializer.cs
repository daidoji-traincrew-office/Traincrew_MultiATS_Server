using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitSignal;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes track circuit entities in the database
/// </summary>
public class TrackCircuitDbInitializer(
    ILogger<TrackCircuitDbInitializer> logger,
    ITrackCircuitRepository trackCircuitRepository,
    ISignalRepository signalRepository,
    ITrackCircuitSignalRepository trackCircuitSignalRepository,
    IGeneralRepository generalRepository,
    ITrackCircuitDepartmentTimeRepository trackCircuitDepartmentTimeRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize track circuits from CSV data
    /// </summary>
    public async Task InitializeTrackCircuitsAsync(List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken = default)
    {
        var trackCircuitNames = (await trackCircuitRepository.GetAllNames(cancellationToken)).ToHashSet();

        var trackCircuits = new List<TrackCircuit>();
        foreach (var item in trackCircuitList)
        {
            if (trackCircuitNames.Contains(item.Name))
            {
                continue;
            }

            trackCircuits.Add(new()
            {
                // Todo: ProtectionZoneの未定義部分がなくなったら、ProtectionZoneのデフォルト値の設定を解除
                ProtectionZone = item.ProtectionZone ?? 99,
                Name = item.Name,
                Type = ObjectType.TrackCircuit,
                // TH00は遅延計算から除外
                StationIdForDelay = item.TargetStation == "TH00" ? null : item.TargetStation,
                TrackCircuitState = new()
                {
                    IsShortCircuit = false,
                    IsLocked = false,
                    TrainNumber = "",
                    IsCorrectionDropRelayRaised = RaiseDrop.Drop,
                    IsCorrectionRaiseRelayRaised = RaiseDrop.Drop,
                    DroppedAt = null,
                    RaisedAt = null
                }
            });
        }

        await generalRepository.AddAll(trackCircuits, cancellationToken);
        logger.LogInformation("Initialized {Count} track circuits", trackCircuits.Count);
    }

    /// <summary>
    ///     Initialize track circuit signal relationships from CSV data
    /// </summary>
    public async Task InitializeTrackCircuitSignalsAsync(List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken = default)
    {
        var trackCircuitNames = trackCircuitList.Select(tc => tc.Name).ToHashSet();
        var trackCircuitEntities = await trackCircuitRepository.GetTrackCircuitsByNamesAsync(
            trackCircuitNames, cancellationToken);

        var allSignalNames = trackCircuitList
            .SelectMany(tc => tc.NextSignalNamesUp.Concat(tc.NextSignalNamesDown))
            .Distinct()
            .ToHashSet();
        var signals = await signalRepository.GetSignalsByNamesAsync(allSignalNames, cancellationToken);

        var trackCircuitIds = trackCircuitEntities.Values.Select(tc => tc.Id).ToHashSet();
        var existingRelationsSet = await trackCircuitSignalRepository.GetExistingRelations(
            trackCircuitIds, cancellationToken);

        var trackCircuitSignals = new List<TrackCircuitSignal>();
        foreach (var trackCircuit in trackCircuitList)
        {
            if (!trackCircuitEntities.TryGetValue(trackCircuit.Name, out var trackCircuitEntity))
            {
                throw new InvalidOperationException($"軌道回路 '{trackCircuit.Name}' が見つかりません。軌道回路信号の初期化に失敗しました。");
            }

            foreach (var signalName in trackCircuit.NextSignalNamesUp)
            {
                if (!signals.TryGetValue(signalName, out var signal))
                {
                    throw new InvalidOperationException($"信号機 '{signalName}' が見つかりません。軌道回路 '{trackCircuit.Name}' の上り信号の初期化に失敗しました。");
                }

                if (existingRelationsSet.Contains((trackCircuitEntity.Id, signal.Name)))
                {
                    continue;
                }

                trackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signal.Name,
                    IsUp = true
                });
            }

            foreach (var signalName in trackCircuit.NextSignalNamesDown)
            {
                if (!signals.TryGetValue(signalName, out var signal))
                {
                    throw new InvalidOperationException($"信号機 '{signalName}' が見つかりません。軌道回路 '{trackCircuit.Name}' の下り信号の初期化に失敗しました。");
                }

                if (existingRelationsSet.Contains((trackCircuitEntity.Id, signal.Name)))
                {
                    continue;
                }

                trackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signal.Name,
                    IsUp = false
                });
            }
        }

        await generalRepository.AddAll(trackCircuitSignals, cancellationToken);
        logger.LogInformation("Initialized track circuit signals");
    }

    /// <summary>
    ///     Initialize track circuit department times from CSV data
    /// </summary>
    public async Task InitializeTrackCircuitDepartmentTimesAsync(
        List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken = default)
    {
        // 対象駅=TH00の軌道回路をフィルタ
        var trackCircuitsNotTH00 = trackCircuitList
            .Where(tc => tc.TargetStation != "TH00")
            .ToList();

        if (trackCircuitsNotTH00.Count == 0)
        {
            logger.LogInformation("No track circuits with non-TH00 target station found");
            return;
        }

        // 軌道回路エンティティを取得
        var trackCircuitNames = trackCircuitsNotTH00.Select(tc => tc.Name).ToHashSet();
        var trackCircuitEntities = await trackCircuitRepository.GetTrackCircuitsByNamesAsync(
            trackCircuitNames, cancellationToken);

        var trackCircuitIds = trackCircuitEntities.Values.Select(tc => tc.Id).ToList();

        // 既存の出発時素レコードを取得してDictionaryに変換
        var existingDepartmentTimes = await trackCircuitDepartmentTimeRepository
            .GetByTrackCircuitIds(trackCircuitIds, cancellationToken);

        var existingDepartmentTimesByKey = existingDepartmentTimes
            .ToDictionary(dt => (dt.TrackCircuitId, dt.CarCount, dt.IsUp));

        var departmentTimesToAdd = new List<TrackCircuitDepartmentTime>();
        var departmentTimesToUpdate = new List<TrackCircuitDepartmentTime>();

        foreach (var item in trackCircuitsNotTH00)
        {
            if (!trackCircuitEntities.TryGetValue(item.Name, out var trackCircuit))
            {
                logger.LogWarning("Track circuit '{Name}' not found for department time initialization", item.Name);
                continue;
            }

            // 上り方向の時素値
            ProcessDepartmentTime(trackCircuit.Id, isUp: true, carCount: 6, item.UpTimeElement6Car);
            ProcessDepartmentTime(trackCircuit.Id, isUp: true, carCount: 4, item.UpTimeElement4Car);
            ProcessDepartmentTime(trackCircuit.Id, isUp: true, carCount: 2, item.UpTimeElement2Car);
            ProcessDepartmentTime(trackCircuit.Id, isUp: true, carCount: 0, item.UpTimeElementPass);

            // 下り方向の時素値
            ProcessDepartmentTime(trackCircuit.Id, isUp: false, carCount: 6, item.DownTimeElement6Car);
            ProcessDepartmentTime(trackCircuit.Id, isUp: false, carCount: 4, item.DownTimeElement4Car);
            ProcessDepartmentTime(trackCircuit.Id, isUp: false, carCount: 2, item.DownTimeElement2Car);
            ProcessDepartmentTime(trackCircuit.Id, isUp: false, carCount: 0, item.DownTimeElementPass);
        }

        // 新規レコードを追加
        if (departmentTimesToAdd.Count > 0)
        {
            await generalRepository.AddAll(departmentTimesToAdd, cancellationToken);
            logger.LogInformation("Added {Count} new track circuit department times", departmentTimesToAdd.Count);
        }

        // 既存レコードを更新
        if (departmentTimesToUpdate.Count > 0)
        {
            await generalRepository.SaveAll(departmentTimesToUpdate, cancellationToken);
            logger.LogInformation("Updated {Count} existing track circuit department times", departmentTimesToUpdate.Count);
        }

        return;

        void ProcessDepartmentTime(
            ulong trackCircuitId,
            bool isUp,
            int carCount,
            int? timeElement)
        {
            if (!timeElement.HasValue)
            {
                return;
            }

            var key = (trackCircuitId, carCount, isUp);

            // 既存レコードを検索 (同一軌道回路ID、同一両数、同一上り下り)
            if (existingDepartmentTimesByKey.TryGetValue(key, out var existing))
            {
                // 既存レコードが見つかった場合、TimeElementを更新
                existing.TimeElement = timeElement.Value;
                departmentTimesToUpdate.Add(existing);
            }
            else
            {
                // 既存レコードがない場合、新規追加
                departmentTimesToAdd.Add(new()
                {
                    TrackCircuitId = trackCircuitId,
                    CarCount = carCount,
                    IsUp = isUp,
                    TimeElement = timeElement.Value
                });
            }
        }
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for TrackCircuitDbInitializer as it requires CSV data
        // Use InitializeTrackCircuitsAsync and InitializeTrackCircuitSignalsAsync instead
        await Task.CompletedTask;
    }
}
