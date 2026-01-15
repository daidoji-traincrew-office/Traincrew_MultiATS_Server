namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

public interface ISwitchingMachineRouteRepository
{
    Task<List<Models.SwitchingMachineRoute>> GetAll();
    Task<List<Models.SwitchingMachineRoute>> GetBySwitchingMachineIds(List<ulong> switchingMachineIds);
    Task<Dictionary<ulong, Models.SwitchingMachineRoute?>> GetFirstByRouteIds(List<ulong> routeIds);
    Task DeleteAll();

    /// <summary>
    /// すべての転てつ器進路のペア(RouteId, SwitchingMachineId)を取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>RouteIdとSwitchingMachineIdのペアのハッシュセット</returns>
    Task<HashSet<(ulong RouteId, ulong SwitchingMachineId)>> GetAllPairsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 転てつ器進路を追加する
    /// </summary>
    /// <param name="switchingMachineRoute">追加する転てつ器進路</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task AddAsync(Models.SwitchingMachineRoute switchingMachineRoute, CancellationToken cancellationToken = default);

    /// <summary>
    /// 変更を保存する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}