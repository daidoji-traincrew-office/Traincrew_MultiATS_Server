using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Server;

public class ServerRepository(ApplicationDbContext context) : IServerRepository
{
    /// <summary>時計の設定行が一意か判定するために最大2行を取得する。</summary>
    public Task<List<ServerState>> GetClockStatesAsync(CancellationToken cancellationToken) =>
        context.ServerStates.OrderBy(s => s.Id).Take(2).ToListAsync(cancellationToken);

    public async Task<ServerState?> GetServerStateAsync()
    {
        return await context.ServerStates.FirstOrDefaultAsync();
    }

    public async Task<ServerMode?> GetModeAsync()
    {
        return await context.ServerStates
            .Select(state => (ServerMode?)state.Mode)
            .FirstOrDefaultAsync();
    }

    public async Task SetServerStateAsync(ServerMode mode)
    {
        var state = await context.ServerStates.FirstOrDefaultAsync();
        if (state == null)
        {
            return;
        }

        state.Mode = mode;
        context.ServerStates.Update(state);
        await context.SaveChangesAsync();
    }

    public async Task<int> GetTimeOffset()
    {
        return await context.ServerStates
            .Select(state => (int?)state.TimeOffset)
            .FirstOrDefaultAsync() ?? 0;
    }

    public async Task SetTimeOffsetAsync(int timeOffset)
    {
        await context.ServerStates
            .ExecuteUpdateAsync(property => property
                .SetProperty(serverState => serverState.TimeOffset, timeOffset)
            );
    }

    /// <summary>時差の比較交換を一つのSQLで行い、複数server_state行も変更しない。</summary>
    public async Task<bool> TrySetTimeOffsetAsync(int expectedTimeOffset, int newTimeOffset, CancellationToken cancellationToken = default)
    {
        var affected = await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.server_state
            SET time_offset = {newTimeOffset}
            WHERE id = (SELECT id FROM public.server_state ORDER BY id LIMIT 1)
                AND time_offset = {expectedTimeOffset}
                AND (SELECT COUNT(*) FROM public.server_state) = 1
            """, cancellationToken);
        return affected == 1;
    }

    public async Task AddServerStateAsync(ServerState serverState, CancellationToken cancellationToken = default)
    {
        context.ServerStates.Add(serverState);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetSwitchMoveTimeAsync(int switchMoveTime)
    {
        await context.ServerStates
            .ExecuteUpdateAsync(property => property
                .SetProperty(serverState => serverState.SwitchMoveTime, switchMoveTime)
            );
    }

    public async Task SetUseOneSecondRelayAsync(bool useOneSecondRelay)
    {
        await context.ServerStates
            .ExecuteUpdateAsync(property => property
                .SetProperty(serverState => serverState.UseOneSecondRelay, useOneSecondRelay)
            );
    }

    public async Task SetIsAllSignalRelayRaisedAsync(RaiseDropWithForce raiseDropWithForce)
    {
        await context.ServerStates
            .ExecuteUpdateAsync(property => property
                .SetProperty(serverState => serverState.IsAllSignalRelayRaised, raiseDropWithForce)
            );
    }

    public async Task<ulong?> GetSelectedDiagramIdAsync()
    {
        return await context.ServerStates
            .Select(state => (ulong?)state.SelectedDiagramId)
            .FirstOrDefaultAsync();
    }

    public async Task SetSelectedDiagramIdAsync(ulong? diaId)
    {
        await context.ServerStates
            .ExecuteUpdateAsync(property => property
                .SetProperty(serverState => serverState.SelectedDiagramId, diaId)
            );
    }

    /// <summary>
    /// 連動サーバーの生存確認用ハートビートのみを更新する
    /// (server_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="heartbeatAt">ハートビート時刻</param>
    public async Task SetInterlockingHeartbeatAtAsync(DateTime heartbeatAt)
    {
        await context.ServerStates
            .ExecuteUpdateAsync(property => property
                .SetProperty(serverState => serverState.InterlockingHeartbeatAt, heartbeatAt)
            );
    }
}
