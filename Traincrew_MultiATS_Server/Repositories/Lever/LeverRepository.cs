using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Lever;

public class LeverRepository(ApplicationDbContext context) : ILeverRepository
{
    public async Task<Models.Lever?> GetLeverByNameWithState(string name)
    {
        return await context.Levers
            .Include(lever => lever.LeverState)
            .FirstOrDefaultAsync(lever => lever.Name == name);
    }

    public async Task<List<ulong>?> GetAllIds()
    {
        return await context.Levers
            .Select(lever => lever.Id)
            .ToListAsync();
    }

    public async Task<List<Models.Lever>> GetAllWithState()
    {
        return await context.Levers
            .Include(lever => lever.LeverState)
            .ToListAsync();
    }
    
    public async Task<List<Models.Lever>> GetByIdsWithState(IEnumerable<ulong> ids)
    {
        return await context.Levers
            .Include(lever => lever.LeverState)
            .Where(lever => ids.Contains(lever.Id))
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsBySwitchingMachineIds(List<ulong> ids)
    {
        return await context.Levers
            .Where(lever => lever.SwitchingMachineId != null && ids.Contains(lever.SwitchingMachineId.Value))
            .Select(lever => lever.Id)
            .ToListAsync();
    }

    /// <summary>
    /// IDを指定してLeverStateのみを取得する(Lever本体はJoinしない)
    /// </summary>
    /// <param name="ids">Lever ID(=LeverState ID)のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>LeverStateのリスト</returns>
    public async Task<List<Models.LeverState>> GetStateByIds(IEnumerable<ulong> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await context.LeverStates
            .Where(state => idList.Contains(state.Id))
            .ToListAsync(cancellationToken);
    }
}