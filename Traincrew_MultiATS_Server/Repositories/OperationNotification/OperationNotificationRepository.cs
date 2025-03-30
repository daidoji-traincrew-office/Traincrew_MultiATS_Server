using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public class OperationNotificationRepository(ApplicationDbContext context) : IOperationNotificationRepository
{
    public async Task<List<OperationNotificationDisplay>> GetAllDisplay()
    {
        return await context.OperationNotificationDisplays
            .Include(d => d.OperationNotificationState)
            .ToListAsync();
    }
    
    public async Task<List<OperationNotificationDisplay?>> GetDisplayByTrackCircuitIds(List<ulong> trackCircuitIds)
    {
        return await context.TrackCircuits
            .Where(tc => trackCircuitIds.Contains(tc.Id))
            .Include(tc => tc.OperationNotificationDisplay)
            .ThenInclude(d => d.OperationNotificationState)
            .Select(tc => tc.OperationNotificationDisplay)
            .ToListAsync();
    }

    public async Task SaveState(OperationNotificationState state)
    {
        var existingState = await context.OperationNotificationStates 
            .FindAsync(state.DisplayName);

        if (existingState != null)
        {
            context.Entry(existingState).CurrentValues.SetValues(state);
        }
        else
        {
            await context.Set<OperationNotificationState>().AddAsync(state);
        }

        await context.SaveChangesAsync();
    }
}
