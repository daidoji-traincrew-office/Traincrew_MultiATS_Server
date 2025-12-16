using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes throw out control entities in the database
/// </summary>
public class ThrowOutControlDbInitializer(
    ApplicationDbContext context,
    ILogger<ThrowOutControlDbInitializer> logger,
    ThrowOutControlCsvLoader csvLoader)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize throw out controls from CSV file (総括制御ペア一覧.csv)
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var records = await csvLoader.LoadAsync(cancellationToken);

        var routesByName = await _context.Routes
            .ToDictionaryAsync(r => r.Name, r => r, cancellationToken);
        var directionRoutesByName = await _context.DirectionRoutes
            .ToDictionaryAsync(dr => dr.Name, dr => dr, cancellationToken);
        var directionSelfControlLeversByName = await _context.DirectionSelfControlLevers
            .ToDictionaryAsync(dscl => dscl.Name, dscl => dscl, cancellationToken);
        var existingThrowOutControls = (await _context.ThrowOutControls
                .Select(toc => new { toc.SourceId, toc.TargetId })
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var addedCount = 0;
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
                        _context.DirectionRoutes.Update(directionRoute);
                    }

                    break;

                default:
                    continue;
            }

            // 既に登録済みの場合はスキップ
            if (existingThrowOutControls.Contains(new { SourceId = sourceRoute.Id, TargetId = target.Id }))
            {
                continue;
            }

            _context.ThrowOutControls.Add(new()
            {
                SourceId = sourceRoute.Id,
                TargetId = target.Id,
                TargetLr = targetLr,
                ConditionLeverId = conditionLeverId,
                ConditionNr = conditionNr,
                ControlType = record.Type
            });
            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} throw out controls", addedCount);
    }
}