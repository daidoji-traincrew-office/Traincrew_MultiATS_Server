using System.Diagnostics.CodeAnalysis;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 起動後不変の interlocking_object + 7派生表の読み取り専用スナップショット。
/// 可変なのは *_state 側だけなので TTL も無効化も持たない。
/// </summary>
/// <remarks>
/// <para>
/// <b>返すエンティティはプロセス全体で共有される実体であり、書き換えてはならない。</b>
/// <see cref="All"/> / <see cref="TryGetById{T}"/> / <see cref="TryGetByName{T}"/> /
/// <see cref="GetByIds{T}"/> / <see cref="GetByNames{T}"/> / <see cref="GetByStationId"/> は
/// いずれもスナップショット内のインスタンスをそのまま返す。プロパティを1つでも書き換えると
/// 全リクエストから見えるマスタが壊れる。状態(*_state)を載せて返したい場合は
/// TrackCircuit.CloneWithState() のような複製を伴うメソッドを通すこと。
/// </para>
/// <para>
/// <b>ここで得たエンティティ(および その複製)を IGeneralRepository.Save に渡してはならない。</b>
/// GeneralRepository.Save は context.Update(entity) を呼び、TPT の全表(interlocking_object 本体を含む)を
/// 全列 UPDATE する。スナップショットは *_state を持たない状態で読んでいるため、
/// 渡すと意図しない上書きが発生する。書き込みは必ず *_state 側のリポジトリ経由で行うこと。
/// </para>
/// </remarks>
public sealed class InterlockingObjectMaster
{
    private readonly Dictionary<ulong, InterlockingObject> byId;
    private readonly Dictionary<string, InterlockingObject> byName;
    private readonly Dictionary<string, InterlockingObject[]> byStationId;

    internal InterlockingObjectMaster(IReadOnlyList<InterlockingObject> objects)
    {
        All = objects;
        byId = objects.ToDictionary(o => o.Id);
        byName = objects.ToDictionary(o => o.Name, StringComparer.Ordinal);
        byStationId = objects
            .Where(o => o.StationId is not null)
            .GroupBy(o => o.StationId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
    }

    public IReadOnlyList<InterlockingObject> All { get; }

    public bool TryGetById<T>(ulong id, [NotNullWhen(true)] out T? value)
        where T : InterlockingObject
    {
        if (byId.TryGetValue(id, out var o) && o is T typed)
        {
            value = typed;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetByName<T>(string name, [NotNullWhen(true)] out T? value)
        where T : InterlockingObject
    {
        if (byName.TryGetValue(name, out var o) && o is T typed)
        {
            value = typed;
            return true;
        }

        value = null;
        return false;
    }

    /// 要件2: 複数取得。入力順を保ち、重複キー・欠番・型不一致は黙って落とす(現 SQL と同じ挙動)。
    public List<T> GetByNames<T>(IEnumerable<string> names) where T : InterlockingObject
    {
        return names
            .Select(name => byName.GetValueOrDefault(name))
            .OfType<T>()
            .Distinct()
            .ToList();
    }

    public List<T> GetByIds<T>(IEnumerable<ulong> ids) where T : InterlockingObject
    {
        return ids
            .Select(id => byId.GetValueOrDefault(id))
            .OfType<T>()
            .Distinct()
            .ToList();
    }

    public IReadOnlyList<InterlockingObject> GetByStationId(string stationId)
        => byStationId.TryGetValue(stationId, out var v) ? v : [];
}

public interface IInterlockingObjectMasterStore
{
    /// <summary>
    /// 現在のスナップショット。<see cref="ReloadAsync"/> より前に読むと例外。
    /// 起動時イーガーロードが前提なので、ホットパスに await もロックも無い。
    /// Crewサーバーでは使用可能だが、Passengerサーバーでは使用できない。
    /// </summary>
    /// <remarks>
    /// 返り値のエンティティの扱いは <see cref="InterlockingObjectMaster"/> の注意書きを参照。
    /// 共有インスタンスなので書き換え禁止、Save にも渡さないこと。
    /// </remarks>
    InterlockingObjectMaster Current { get; }

    /// <summary>
    /// DBから読み直して <see cref="Current"/> を差し替える。
    /// 読み込み中も読者は旧スナップショットを見続け、差し替えは原子的。
    /// </summary>
    Task<InterlockingObjectMaster> ReloadAsync(CancellationToken ct = default);

    /// <summary>
    /// 未ロード状態に戻す。以後 <see cref="Current"/> は例外を投げる。
    /// </summary>
    void Unload();
}

public class InterlockingObjectMasterStore(
    IServiceScopeFactory serviceScopeFactory,
    IMutexRepository mutexRepository
) : IInterlockingObjectMasterStore
{
    private InterlockingObjectMaster? _current;

    public InterlockingObjectMaster Current
        => Volatile.Read(ref _current)
           ?? throw new InvalidOperationException(
               "InterlockingObjectMaster が未ロードです。InitDbHostedService の ReloadAsync より前に呼ばれています。");

    public async Task<InterlockingObjectMaster> ReloadAsync(CancellationToken ct = default)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingObjectMaster), ct);
        // 読み込み中も読者は旧スナップショットを見続ける。差し替えは原子的。
        var snapshot = await LoadAsync(ct);
        Volatile.Write(ref _current, snapshot);
        return snapshot;
    }

    private async Task<InterlockingObjectMaster> LoadAsync(CancellationToken ct = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var interlockingObjectRepository = scope.ServiceProvider.GetRequiredService<IInterlockingObjectRepository>();
        var interlockingObjects = await interlockingObjectRepository.GetAllAsync(ct);
        return new(interlockingObjects);
    }

    /// 空スナップショットではなく未ロード状態に戻す。停止後に読んだら黙って0件ではなく例外にする。
    public void Unload()
        => Volatile.Write(ref _current, null);
}
