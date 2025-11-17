using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Server;

public class ServerRepository(ApplicationDbContext context) : IServerRepository
{
    public async Task<ServerState?> GetServerStateAsync()
    {
        return await context.ServerStates.FirstOrDefaultAsync();
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
            .Select(state => state.TimeOffset)
            .DefaultIfEmpty(0)
            .FirstAsync();
    }

    public async Task SetTimeOffsetAsync(int timeOffset)
    {
        await context.ServerStates
            .ExecuteUpdateAsync(property => property
                .SetProperty(serverState => serverState.TimeOffset, timeOffset)
            );
    }
}