using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.BroadcastMaster;
using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>信号機のマスタ列。State・ナビゲーションは持たない。</summary>
public sealed record BroadcastSignalMaster(
    string Name,
    string TypeName,
    string? StationId,
    SignalType Type,
    ulong? TrackCircuitId,
    ulong? DirectionRouteLeftId,
    ulong? DirectionRouteRightId,
    LR? Direction);

/// <summary>信号機と進路の対応。進路そのもの(連動オブジェクト)はIDで引く。</summary>
public sealed record BroadcastSignalRouteMaster(string SignalName, ulong RouteId);

/// <summary>列番窓のマスタ列。表示駅のIDを含む。</summary>
public sealed record BroadcastTtcWindowMaster(
    string Name,
    string StationId,
    TtcWindowType Type,
    IReadOnlyList<string> DisplayStationIds);

/// <summary>着点ボタンのマスタ列。</summary>
public sealed record BroadcastDestinationButtonMaster(string Name, string StationId);

/// <summary>
/// 配信が毎ティック使う、起動後不変のマスタ(interlocking_object系以外)の読み取り専用スナップショット。
/// </summary>
/// <remarks>
/// IDとマスタ列だけを持つ不変のレコードで構成する。可変なState・ナビゲーション・連動オブジェクト
/// (進路・軌道回路・方向進路)は持たず、それらは <see cref="InterlockingObjectMaster"/> 側にIDで引く。
/// コレクションはすべて読み取り専用。配信側で加工する場合は必ずコピーすること。
/// </remarks>
public sealed class BroadcastMasterData(
    IReadOnlyList<string> stationIds,
    IReadOnlyList<BroadcastSignalMaster> signals,
    IReadOnlyDictionary<string, IReadOnlyList<string>> nextSignalNamesBySignalName,
    IReadOnlyDictionary<string, IReadOnlyList<ulong>> routeIdsBySignalName,
    IReadOnlyList<BroadcastTtcWindowMaster> ttcWindows,
    IReadOnlyList<BroadcastDestinationButtonMaster> destinationButtons)
{
    /// <summary>駅(IsStation)のID。ID昇順。</summary>
    public IReadOnlyList<string> StationIds { get; } = stationIds;

    /// <summary>信号機。名前の昇順。</summary>
    public IReadOnlyList<BroadcastSignalMaster> Signals { get; } = signals;

    /// <summary>信号機名 → 1つ先(depth=1)の信号機名。</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> NextSignalNamesBySignalName { get; } =
        nextSignalNamesBySignalName;

    /// <summary>信号機名 → 対応する進路ID。</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ulong>> RouteIdsBySignalName { get; } = routeIdsBySignalName;

    /// <summary>列番窓。名前の昇順。</summary>
    public IReadOnlyList<BroadcastTtcWindowMaster> TtcWindows { get; } = ttcWindows;

    /// <summary>着点ボタン。名前の昇順。</summary>
    public IReadOnlyList<BroadcastDestinationButtonMaster> DestinationButtons { get; } = destinationButtons;
}

public interface IBroadcastMasterStore
{
    /// <summary>
    /// 現在のスナップショット。<see cref="ReloadAsync"/> より前に読むと例外。
    /// 起動時にだけロードする(実行中の再ロード・無効化は無い。マスタを更新したら再起動する)。
    /// Crewサーバーでのみ使用する。Passengerサーバーではロードされないので使用できない。
    /// </summary>
    BroadcastMasterData Current { get; }

    Task<BroadcastMasterData> ReloadAsync(CancellationToken ct = default);

    /// <summary>未ロード状態に戻す。以後 <see cref="Current"/> は例外を投げる。</summary>
    void Unload();
}

public class BroadcastMasterStore(
    IServiceScopeFactory serviceScopeFactory,
    IMutexRepository mutexRepository
) : IBroadcastMasterStore
{
    private BroadcastMasterData? _current;

    public BroadcastMasterData Current
        => Volatile.Read(ref _current)
           ?? throw new InvalidOperationException(
               "BroadcastMasterData が未ロードです。InitDbHostedService の ReloadAsync より前に呼ばれています。");

    public async Task<BroadcastMasterData> ReloadAsync(CancellationToken ct = default)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(BroadcastMasterData), ct);
        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBroadcastMasterRepository>();
        var snapshot = await repository.LoadAsync(ct);
        Volatile.Write(ref _current, snapshot);
        return snapshot;
    }

    public void Unload()
        => Volatile.Write(ref _current, null);
}
