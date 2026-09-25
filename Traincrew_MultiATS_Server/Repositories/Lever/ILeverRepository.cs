namespace Traincrew_MultiATS_Server.Repositories.Lever;

public interface ILeverRepository
{
    Task<List<ulong>> GetIdsBySwitchingMachineIds(List<ulong> ids);

    Task<Models.Lever?> GetLeverByNameWithState(string name);

    Task<List<ulong>?> GetAllIds();
    
    Task<List<Models.Lever>> GetAllWithState();
    
    Task<List<Models.Lever>> GetByIdsWithState(IEnumerable<ulong> ids);

    /// <summary>
    /// IDを指定してLeverStateのみを取得する(Lever本体はJoinしない)
    /// </summary>
    /// <param name="ids">Lever ID(=LeverState ID)のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>LeverStateのリスト</returns>
    Task<List<Models.LeverState>> GetStateByIds(IEnumerable<ulong> ids, CancellationToken cancellationToken = default);
}