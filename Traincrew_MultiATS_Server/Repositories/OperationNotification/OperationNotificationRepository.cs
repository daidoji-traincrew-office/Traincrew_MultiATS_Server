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
            .Include(tc => tc.OperationNotificationDisplay)
            .ThenInclude(d => d.TrackCircuits)
            .Select(tc => tc.OperationNotificationDisplay)
            .ToListAsync();
    }

    public async Task SetNoneWhereKaijoAndOperatedBeforeOrEqual(DateTime operatedAt)
    {
        await context.OperationNotificationStates
            .Where(s => s.Type == OperationNotificationType.Kaijo && s.OperatedAt <= operatedAt)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ons => ons.Type, OperationNotificationType.None)
                .SetProperty(ons => ons.Content, string.Empty)
            );
    }
}