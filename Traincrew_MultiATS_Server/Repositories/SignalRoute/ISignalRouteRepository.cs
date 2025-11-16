namespace Traincrew_MultiATS_Server.Repositories.SignalRoute;

public interface ISignalRouteRepository
{
    Task<Dictionary<string, List<Models.Route>>> GetRoutesBySignalNames(IEnumerable<string> signalNames);
    /// <summary>
    /// 複数の進路IDで信号機-進路の関連を取得する
    /// </summary>
    /// <param name="routeIds">進路IDのリスト</param>
    /// <returns>信号機-進路の関連リスト</returns>
    Task<List<Models.SignalRoute>> GetByRouteIds(IEnumerable<ulong> routeIds);
}