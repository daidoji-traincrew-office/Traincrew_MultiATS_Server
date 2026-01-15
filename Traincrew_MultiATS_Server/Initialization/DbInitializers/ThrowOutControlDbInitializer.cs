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
    : BaseDbInitializer(logger)
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
                _logger.LogWarning("総括制御元が見つかりません。処理をスキップします。: {SourceLever}", record.SourceLever);
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
                        continue;
                    }

                    target = targetRoute;
                    break;

                case ThrowOutControlType.Direction:
                    // 方向の場合、方向進路を探す
                    if (string.IsNullOrEmpty(record.LeverCondition))
                    {
                        continue;
                    }

                    // record.LeverConditionから方向進路名を取得（末尾を除いてFに置換）
                    var directionRouteName = record.TargetLever[..^1] + "F";
                    if (!directionRoutesByName.TryGetValue(directionRouteName, out var directionRoute))
                    {
                        continue;
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
        await generalRepository.AddAll(throwOutControlsToAdd);
        _logger.LogInformation("Initialized {Count} throw out controls", throwOutControlsToAdd.Count);
    }
}