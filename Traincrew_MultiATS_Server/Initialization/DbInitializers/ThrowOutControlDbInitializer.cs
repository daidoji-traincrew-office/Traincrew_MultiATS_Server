using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes throw out control entities in the database
/// </summary>
public class ThrowOutControlDbInitializer(
    ILogger<ThrowOutControlDbInitializer> logger,
    ThrowOutControlCsvLoader csvLoader,
    IRouteRepository routeRepository,
    IDirectionRouteRepository directionRouteRepository,
    IDirectionSelfControlLeverRepository directionSelfControlLeverRepository,
    IThrowOutControlRepository throwOutControlRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize throw out controls from CSV file (総括制御ペア一覧.csv)
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var records = await csvLoader.LoadAsync(cancellationToken);

        var routesByName = await routeRepository.GetByNames(cancellationToken);
        var directionRoutesByName = await directionRouteRepository.GetByNamesAsDictionaryAsync(cancellationToken);
        var directionSelfControlLeversByName = await directionSelfControlLeverRepository.GetByNamesAsDictionaryAsync(cancellationToken);
        var existingThrowOutControls = await throwOutControlRepository.GetAllPairsAsync(cancellationToken);

        var throwOutControlsToAdd = new List<ThrowOutControl>();
        var directionRoutesToUpdate = new List<DirectionRoute>();

        foreach (var record in records)
        {
            // 総括制御元の進路を取得
            if (!routesByName.TryGetValue(record.SourceLever, out var sourceRoute))
            {
                logger.LogWarning("総括制御元が見つかりません。処理をスキップします。: {SourceLever}", record.SourceLever);
                continue;
            }

            InterlockingObject target;
            LR? targetLr = null;
            NR? conditionNr = null;
            ulong? conditionLeverId = null;

            // 総括種類による分岐処理
            switch (record.Type)
            {
                case ThrowOutControlType.WithLever:
                case ThrowOutControlType.WithoutLever:
                    // てこあり/てこナシの場合、総括制御先は通常の進路
                    if (!routesByName.TryGetValue(record.TargetLever, out var targetRoute))
                    {
                        throw new InvalidOperationException($"総括制御先の進路 '{record.TargetLever}' が見つかりません。総括制御元 '{record.SourceLever}' の初期化に失敗しました。");
                    }

                    target = targetRoute;
                    break;

                case ThrowOutControlType.Direction:
                    // 方向の場合、方向進路を探す
                    if (string.IsNullOrEmpty(record.LeverCondition))
                    {
                        throw new InvalidOperationException($"総括制御元 '{record.SourceLever}' のてこ条件が設定されていません。方向総括制御の初期化に失敗しました。");
                    }

                    // record.LeverConditionから方向進路名を取得（末尾を除いてFに置換）
                    var directionRouteName = record.TargetLever[..^1] + "F";
                    if (!directionRoutesByName.TryGetValue(directionRouteName, out var directionRoute))
                    {
                        throw new InvalidOperationException($"方向進路 '{directionRouteName}' が見つかりません。総括制御元 '{record.SourceLever}' の初期化に失敗しました。");
                    }

                    target = directionRoute;
                    // 総括制御先のてこ名の末尾から方向を判定
                    targetLr = record.TargetLever.EndsWith("L") ? LR.Left : LR.Right;

                    // 開放てこを取得
                    var directionSelfControlLeverName = record.LeverCondition.TrimEnd('N');
                    if (directionSelfControlLeversByName.TryGetValue(directionSelfControlLeverName,
                            out var directionSelfControlLever))
                    {
                        conditionLeverId = directionSelfControlLever.Id;
                        directionRoute.DirectionSelfControlLeverId = conditionLeverId;
                        conditionNr = NR.Normal;
                        directionRoutesToUpdate.Add(directionRoute);
                    }

                    break;

                default:
                    logger.LogWarning("サポートされていない総括制御タイプです。総括制御元: {SourceLever}, タイプ: {Type}", record.SourceLever, record.Type);
                    continue;
            }

            // 既に登録済みの場合はスキップ
            if (existingThrowOutControls.Contains((sourceRoute.Id, target.Id)))
            {
                continue;
            }

            throwOutControlsToAdd.Add(new()
            {
                SourceId = sourceRoute.Id,
                TargetId = target.Id,
                TargetLr = targetLr,
                ConditionLeverId = conditionLeverId,
                ConditionNr = conditionNr,
                ControlType = record.Type
            });
        }

        // Update direction routes
        foreach (var directionRoute in directionRoutesToUpdate)
        {
            directionRouteRepository.Update(directionRoute);
        }
        await directionRouteRepository.SaveChangesAsync(cancellationToken);

        // Add throw out controls
        await generalRepository.AddAll(throwOutControlsToAdd, cancellationToken);
        logger.LogInformation("Initialized {Count} throw out controls", throwOutControlsToAdd.Count);
    }
}