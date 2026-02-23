using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.ApproachAlertCondition;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.LockCondition;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

public class ApproachAlertConditionDbInitializer(
    ILogger<ApproachAlertConditionDbInitializer> logger,
    ApplicationDbContext context,
    IDateTimeRepository dateTimeRepository,
    ILoggerFactory loggerFactory,
    IApproachAlertConditionRepository approachAlertConditionRepository,
    ILockConditionRepository lockConditionRepository)
{
    // 駅名 → 駅ID（CSVの備考欄から導出）
    private static readonly Dictionary<string, string> StationNameToId = new()
    {
        { "館浜駅", "TH76" },
        { "駒野駅", "TH75" },
        { "津崎駅", "TH71" },
        { "浜園駅", "TH70" },
        { "新野崎駅", "TH67" },
        { "江ノ原検車区", "TH66S" },
        { "大道寺駅", "TH65" },
        { "藤江駅", "TH64" },
        { "水越駅", "TH63" },
        { "高見沢駅", "TH62" },
        { "日野森駅", "TH61" },
        { "西赤山駅", "TH59" },
        { "赤山町駅", "TH58" },
    };

    // 接近警報用隣接駅マップ（連動図表のStationIdMapとは異なる）
    private static readonly Dictionary<string, List<string>> ApproachAlertStationIdMap = new()
    {
        { "TH76", ["TH75"] },
        { "TH75", ["TH76"] },
        { "TH71", ["TH70"] },
        { "TH70", ["TH71"] },
        { "TH67", ["TH66S"] },
        { "TH66S", ["TH67", "TH65"] },
        { "TH65", ["TH66S", "TH64"] },
        { "TH64", ["TH65", "TH63"] },
        { "TH63", ["TH64", "TH62"] },
        { "TH62", ["TH63", "TH61"] },
        { "TH61", ["TH62", "TH59"] },
        { "TH59", ["TH61", "TH58"] },
        { "TH58", ["TH59", "TH57"] },
    };

    public async Task InitializeAsync(
        List<ApproachAlertConditionCsv> csvData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("接近警報鳴動条件の初期化を開始します");

        foreach (var row in csvData)
        {
            if (!StationNameToId.TryGetValue(row.StationName, out var stationId))
            {
                logger.LogWarning("未知の駅名: {Name}", row.StationName);
                continue;
            }

            // 上り条件（「メモ：」始まりはスキップ）
            if (!string.IsNullOrWhiteSpace(row.UpCondition)
                && !row.UpCondition.TrimStart().StartsWith("メモ"))
            {
                await ProcessConditionStringAsync(
                    stationId, true, row.UpCondition, cancellationToken);
            }

            // 下り条件
            if (!string.IsNullOrWhiteSpace(row.DownCondition))
            {
                await ProcessConditionStringAsync(
                    stationId, false, row.DownCondition, cancellationToken);
            }
        }

        logger.LogInformation("接近警報鳴動条件の初期化が完了しました");
    }

    private async Task ProcessConditionStringAsync(
        string stationId, bool isUp, string conditionStr,
        CancellationToken cancellationToken)
    {
        var parser = new DbRendoTableInitializer(
            stationId, [],
            context, dateTimeRepository,
            loggerFactory.CreateLogger<DbRendoTableInitializer>(),
            cancellationToken,
            ApproachAlertStationIdMap);

        var lockItems = parser.CalcLockItems(conditionStr, false);
        if (lockItems.Count == 0) return;

        var otherStations = (ApproachAlertStationIdMap.GetValueOrDefault(stationId) ?? [])
            .Where(s => s != "閉そく")
            .ToList();

        var searcher = new InterlockingObjectSearcher(stationId, otherStations, context, cancellationToken);
        await searcher.InitializeAsync();

        // 複数エントリは "and" ノードのChildrenに格納される
        List<DbRendoTableInitializer.LockItem> entries;
        var root = lockItems[0];
        entries = root.Name == "and" ? root.Children : lockItems;

        foreach (var entry in entries)
        {
            await RegisterEntryAsync(stationId, isUp, entry, searcher, cancellationToken);
        }
    }

    private async Task RegisterEntryAsync(
        string stationId, bool isUp,
        DbRendoTableInitializer.LockItem entry,
        InterlockingObjectSearcher searcher,
        CancellationToken cancellationToken)
    {
        // "or"ノード = 但条件あり: Children[0]=TC、Children[1]=not条件
        var isButCondition = entry.Name == "or";
        var leftCondition = isButCondition ? entry.Children[0] : entry;
        var tcItems = leftCondition.Name == "and" ? leftCondition.Children : [leftCondition];
        foreach (var tcItem in tcItems)
        {
            var tcObjects = await searcher.SearchObjectsForApproachLockAsync(tcItem);
            if (tcObjects.Count == 0)
            {
                logger.LogWarning("軌道回路が見つかりません: {StationId} {Name}", tcItem.StationId, tcItem.Name);
                return;
            }

            var trackCircuitId = tcObjects[0].Id;

            var approachAlertCondition = await approachAlertConditionRepository.AddAndSaveAsync(
                new()
                {
                    StationId = stationId,
                    IsUp = isUp,
                    TrackCircuitId = trackCircuitId
                },
                cancellationToken);

            if (!isButCondition)
            {
                continue;
            }

            var conditionItem = entry.Children[1];
            await RegisterLockConditionTreeAsync(
                conditionItem,
                approachAlertCondition.Id,
                null,
                searcher,
                cancellationToken);
        }
    }

    /// <summary>
    /// lock_conditionツリーを再帰的に作成する。
    /// 全ノードに approach_alert_condition_id をセット（lock_id=NULL）。
    /// </summary>
    private async Task RegisterLockConditionTreeAsync(
        DbRendoTableInitializer.LockItem item,
        ulong approachAlertConditionId,
        ulong? parentId,
        InterlockingObjectSearcher searcher,
        CancellationToken cancellationToken)
    {
        var conditionType = item.Name switch
        {
            "and" => LockConditionType.And,
            "or" => LockConditionType.Or,
            "not" => LockConditionType.Not,
            _ => LockConditionType.Object
        };

        if (conditionType == LockConditionType.Object)
        {
            var objects = await searcher.SearchOtherObjectsAsync(item);
            if (objects.Count == 0)
            {
                logger.LogWarning("連動オブジェクトが見つかりません: {StationId} {Name}", item.StationId, item.Name);
                return;
            }

            await lockConditionRepository.AddObjectAndSaveAsync(
                new()
                {
                    LockId = null,
                    ApproachAlertConditionId = approachAlertConditionId,
                    ParentId = parentId,
                    Type = LockConditionType.Object,
                    ObjectId = objects[0].Id,
                    IsReverse = item.IsReverse,
                    IsSingleLock = item.isLocked,
                    TrainNumberCondition = item.TrainNumberCondition
                },
                cancellationToken);
        }
        else
        {
            var node = await lockConditionRepository.AddAndSaveAsync(
                new()
                {
                    LockId = null,
                    ApproachAlertConditionId = approachAlertConditionId,
                    ParentId = parentId,
                    Type = conditionType
                },
                cancellationToken);

            foreach (var child in item.Children)
            {
                await RegisterLockConditionTreeAsync(
                    child, approachAlertConditionId, node.Id,
                    searcher, cancellationToken);
            }
        }
    }
}
