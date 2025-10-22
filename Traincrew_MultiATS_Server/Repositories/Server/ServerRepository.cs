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
        var state = await context.ServerStates.FirstOrDefaultAsync();
        return state?.TimeOffset ?? 0;
    }

    public async Task SetTimeOffsetAsync(int timeOffset)
    {
        var state = await context.ServerStates.FirstOrDefaultAsync();
        if (state == null)
        {
            return;
        }
        state.TimeOffset = timeOffset;
        context.ServerStates.Update(state);
        await context.SaveChangesAsync();
    }
}
