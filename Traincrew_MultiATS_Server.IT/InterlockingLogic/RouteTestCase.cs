using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.IT.InterlockingLogic;

/// <summary>
/// 進路構成テストケースを表すDTO
/// </summary>
public record RouteTestCase
{
    /// <summary>
    /// 駅ID
    /// </summary>
    public required string StationId { get; init; }

    /// <summary>
    /// 駅名
    /// </summary>
    public required string StationName { get; init; }

    /// <summary>
    /// 進路ID
    /// </summary>
    public required ulong RouteId { get; init; }

    /// <summary>
    /// 進路名
    /// </summary>
    public required string RouteName { get; init; }

    /// <summary>
    /// てこ名
    /// </summary>
    public required string LeverName { get; init; }

    /// <summary>
    /// てこの操作方向
    /// </summary>
    public required LCR LeverDirection { get; init; }

    /// <summary>
    /// 着点ボタン名（nullの場合もある）
    /// </summary>
    public string? DestinationButtonName { get; init; }

    /// <summary>
    /// 対応する信号機名
    /// </summary>
    public required string SignalName { get; init; }

    /// <summary>
    /// てこなし総括先の信号機名リスト（てこなし総括元の場合のみ）
    /// </summary>
    public List<string>? ThrowOutControlTargetSignals { get; init; }

    /// <summary>
    /// テストケース名を生成
    /// </summary>
    public string GetTestCaseName()
        => $"{StationId}_{RouteName}のてこと着点を倒して進路が開通すること";
}