using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Services;

public sealed class OperationNotificationMaster
{
    private readonly IReadOnlyDictionary<ulong, string> _nameByTrackCircuitId;
    private readonly IReadOnlyDictionary<string, HashSet<ulong>> _trackCircuitIdsByName;

    internal OperationNotificationMaster(IReadOnlyList<TrackCircuit> trackCircuits)
    {
        _nameByTrackCircuitId = trackCircuits
            .Where(x => x.OperationNotificationDisplayName != null)
            .ToDictionary(
                x => x.Id,
                x => x.OperationNotificationDisplayName!
            );
        _trackCircuitIdsByName = _nameByTrackCircuitId
            .GroupBy(x => x.Value)
            .ToDictionary(
                x => x.Key,
                x => new HashSet<ulong>(x.Select(y => y.Key)),
                StringComparer.Ordinal
            );
    }

    public string? TryGetDisplayName(IReadOnlyList<ulong> trackCircuitIds)
    {
        // そもそも軌道回路が渡されてない
        if (trackCircuitIds.Count == 0)
        {
            return null;
        }
        // 軌道回路に該当する告知器がない
        if (!_nameByTrackCircuitId.TryGetValue(trackCircuitIds[0], out var name))
        {
            return null;
        }
        var ids = _trackCircuitIdsByName[name];
        if (ids.Count != trackCircuitIds.Count)
        {
            return null; // 入りきっていない
        }
        // 別告知器の軌道回路が混ざっていないか
        return trackCircuitIds.All(ids.Contains) ? name : null;
    }
}

public interface IOperationNotificationMasterStore
{
    OperationNotificationMaster Current { get; }
    Task<OperationNotificationMaster> ReloadAsync(CancellationToken ct = default);
    void Unload();
}

public class OperationNotificationMasterStore(
    IInterlockingObjectMasterStore interlockingObjectMasterStore,
    IMutexRepository mutexRepository) : IOperationNotificationMasterStore
{
    private OperationNotificationMaster? _current;

    public OperationNotificationMaster Current
        => Volatile.Read(ref _current)
           ?? throw new InvalidOperationException(
               "OperationNotificationMaster が未ロードです。InitDbHostedService の ReloadAsync より前に呼ばれています。");

    public async Task<OperationNotificationMaster> ReloadAsync(CancellationToken ct = default)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(OperationNotificationMaster), ct);
        // 読み込み中も読者は旧スナップショットを見続ける。差し替えは原子的。
        var snapshot = Load();
        Volatile.Write(ref _current, snapshot);
        return snapshot;
    }

    private OperationNotificationMaster Load()
    {
        // 告知器のある軌道回路の抽出は OperationNotificationMaster のコンストラクタで行う
        var trackCircuits = interlockingObjectMasterStore.Current.All
            .OfType<TrackCircuit>()
            .ToList();
        return new(trackCircuits);
    }

    /// 空スナップショットではなく未ロード状態に戻す。停止後に読んだら黙って0件ではなく例外にする。
    public void Unload()
        => Volatile.Write(ref _current, null);
}
