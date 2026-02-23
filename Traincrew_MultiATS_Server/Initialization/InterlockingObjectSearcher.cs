using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Initialization;

public class InterlockingObjectSearcher
{
    private const string NameClosure = "閉そく";
    private const string PrefixTrackCircuitDown = "下り";
    private const string PrefixTrackCircuitUp = "上り";

    private readonly string _stationId;
    private readonly List<string> _otherStations;
    private readonly ApplicationDbContext _context;
    private readonly CancellationToken _cancellationToken;

    private Dictionary<string, SwitchingMachine> _switchingMachines = new();
    private Dictionary<string, InterlockingObject> _otherObjects = new();
    private Dictionary<string, List<ulong>> _routeIdsByLeverName = new();
    private Dictionary<string, List<ulong>> _routeIdsByButtonName = new();
    private Dictionary<ulong, List<ThrowOutControl>> _throwOutControlBySourceId = new();
    private Dictionary<ulong, Route> _routesById = new();

    public InterlockingObjectSearcher(
        string stationId,
        List<string> otherStations,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        _stationId = stationId;
        _otherStations = otherStations;
        _context = context;
        _cancellationToken = cancellationToken;
    }

    public async Task InitializeAsync()
    {
        var interlockingObjects = await _context.InterlockingObjects
            .Where(io => io.Name.StartsWith(_stationId) || _otherStations.Any(s => io.Name.StartsWith(s)))
            .ToListAsync(_cancellationToken);

        var leverNamesById = await _context.Levers
            .ToDictionaryAsync(l => l.Id, l => l.Name, _cancellationToken);
        var routeLeverDestinationButtons = await _context.RouteLeverDestinationButtons
            .ToListAsync(_cancellationToken);
        var throwOutControl = await _context.ThrowOutControls
            .ToListAsync(_cancellationToken);

        _switchingMachines = interlockingObjects
            .OfType<SwitchingMachine>()
            .ToDictionary(sm => sm.Name, sm => sm);

        _otherObjects = interlockingObjects
            .Where(io => io is not SwitchingMachine)
            .ToDictionary(io => io.Name, io => io);

        var routes = interlockingObjects
            .OfType<Route>()
            .ToList();

        _routesById = routes.ToDictionary(r => r.Id, r => r);

        _routeIdsByLeverName = routeLeverDestinationButtons
            .GroupBy(x => x.LeverId)
            .ToDictionary(
                g => leverNamesById[g.Key],
                g => g.Select(x => x.RouteId).ToList()
            );

        _routeIdsByButtonName = routeLeverDestinationButtons
            .Where(x => x.DestinationButtonName != null)
            .GroupBy(x => x.DestinationButtonName!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.RouteId).ToList()
            );

        _throwOutControlBySourceId = throwOutControl
            .GroupBy(toc => toc.SourceId)
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
    }

    public Task<List<InterlockingObject>> SearchSwitchingMachineAsync(DbRendoTableInitializer.LockItem item)
    {
        var targetObject =
            _switchingMachines.GetValueOrDefault(
                DbRendoTableInitializer.CalcSwitchingMachineName(item.Name, item.StationId));
        List<InterlockingObject> result = targetObject != null ? [targetObject] : [];
        return Task.FromResult(result);
    }

    public async Task<List<InterlockingObject>> SearchOtherObjectsAsync(DbRendoTableInitializer.LockItem item)
    {
        // 転てつ器(数字のみの名前)の場合はこちら
        var numericOnlyMatch = DbRendoTableInitializer.RegexNumericOnly().IsMatch(item.Name);
        if (numericOnlyMatch)
        {
            var switchingMachine = _switchingMachines.GetValueOrDefault(
                DbRendoTableInitializer.CalcSwitchingMachineName(item.Name, item.StationId));
            if (switchingMachine != null)
            {
                return [switchingMachine];
            }
        }

        // 進路(単一) or 軌道回路の場合はこちら
        var key = DbRendoTableInitializer.ConvertHalfWidthToFullWidth(
            DbRendoTableInitializer.CalcRouteName(item.Name, "", item.StationId));
        var value = _otherObjects.GetValueOrDefault(key);
        if (value != null)
        {
            return [value];
        }

        // 方向進路の場合はこちら
        var directionRoute = await SearchDirectionRoutesAsync(item);
        if (directionRoute != null)
        {
            return [directionRoute];
        }

        // てこ名指定の場合はこちら=>そのてこを始点としたすべての進路を取得
        var leverFullMatch = DbRendoTableInitializer.RegexLeverParseFullMatch().Match(item.Name);
        if (leverFullMatch.Success)
        {
            var leverName = DbRendoTableInitializer.CalcLeverName(
                leverFullMatch.Groups[1].Value + leverFullMatch.Groups[3].Value, item.StationId);
            var routeIds = _routeIdsByLeverName.GetValueOrDefault(leverName);
            if (routeIds != null)
            {
                return routeIds.Select(InterlockingObject (r) => _routesById[r]).ToList();
            }
        }

        // 統括進路はこちら
        var leverMatch = DbRendoTableInitializer.RegexLeverParse().Match(item.Name);
        if (leverMatch.Success)
        {
            // てこ
            var leverName = DbRendoTableInitializer.CalcLeverName(
                leverMatch.Groups[1].Value + leverMatch.Groups[3].Value, item.StationId);
            // 着点ボタン
            var buttonName = DbRendoTableInitializer.CalcButtonName(
                item.Name[(leverMatch.Index + leverMatch.Length)..],
                item.StationId);

            // 統括制御から、該当する進路を導き出す
            // てこに該当する進路すべて
            var startRouteIds = _routeIdsByLeverName.GetValueOrDefault(leverName, []);
            // 該当する統括制御を選ぶ(てこに該当する進路=>統括制御=>着点てこに該当する進路)
            var targetThrowOutControls = startRouteIds
                .SelectMany(r => _throwOutControlBySourceId.GetValueOrDefault(r, []))
                .Where(toc => _routeIdsByButtonName[buttonName].Contains(toc.TargetId))
                .ToList();
            var targetThrowOutControl = targetThrowOutControls.FirstOrDefault();
            if (targetThrowOutControls.Count >= 2)
            {
                throw new InvalidOperationException($"統括制御が2つ以上見つかりました: {item.Name}");
            }

            if (targetThrowOutControl != null)
            {
                var startRoute = _routesById[targetThrowOutControl.SourceId];
                var endRoute = _routesById[targetThrowOutControl.TargetId];
                return [startRoute, endRoute];
            }
        }

        return [];
    }

    public async Task<List<InterlockingObject>> SearchClosureTrackCircuitAsync(DbRendoTableInitializer.LockItem item)
    {
        var match = DbRendoTableInitializer.RegexClosureTrackCircuitParse().Match(item.Name);
        string trackCircuitName;
        // 閉塞軌道回路
        if (match.Success)
        {
            var trackCircuitNumber = int.Parse(match.Groups[1].Value);
            var prefix = trackCircuitNumber % 2 == 0 ? PrefixTrackCircuitUp : PrefixTrackCircuitDown;
            trackCircuitName = $"{prefix}{trackCircuitNumber}T";
        }
        // 単線の諸々軌道回路
        else
        {
            trackCircuitName = item.Name;
        }

        var trackCircuit = await _context.TrackCircuits
            .FirstOrDefaultAsync(tc => tc.Name == trackCircuitName, _cancellationToken);
        if (trackCircuit == null)
        {
            return [];
        }

        return [trackCircuit];
    }

    public async Task<List<InterlockingObject>> SearchObjectsForApproachLockAsync(
        DbRendoTableInitializer.LockItem item)
    {
        if (item.StationId == NameClosure)
        {
            return await SearchClosureTrackCircuitAsync(item);
        }

        var result = await SearchOtherObjectsAsync(item);
        if (result.Count > 0)
        {
            return result;
        }

        // 接近鎖錠の場合、閉塞軌道回路も探す
        return await SearchClosureTrackCircuitAsync(item);
    }

    private Task<InterlockingObject?> SearchDirectionRoutesAsync(DbRendoTableInitializer.LockItem item)
    {
        // 方向進路
        if (!(item.Name.EndsWith('L') || item.Name.EndsWith('R')))
        {
            return Task.FromResult<InterlockingObject?>(null);
        }

        var key = DbRendoTableInitializer.CalcDirectionLeverName(item.Name[..^1], item.StationId);
        return Task.FromResult(_otherObjects.GetValueOrDefault(key));
    }
}
