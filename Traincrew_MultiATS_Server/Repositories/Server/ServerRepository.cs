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

    public async Task SetIsAllSignalRelayRaisedAsync(RaiseDropWithForce raiseDropWithForce)
    {
        var state = await context.ServerStates.FirstOrDefaultAsync();
        if (state == null)
        {
            return;
        }
        state.IsAllSignalRelayRaised = raiseDropWithForce;
        context.ServerStates.Update(state);
        await context.SaveChangesAsync();
    }
}
