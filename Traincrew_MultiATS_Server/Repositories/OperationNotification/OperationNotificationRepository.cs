using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public class OperationNotificationRepository(ApplicationDbContext context) : IOperationNotificationRepository
{
    public async Task<List<Models.OperationNotificationDisplay>> GetAllDisplay()
    {
        return await context.OperationNotificationDisplays
            .Include(d => d.OperationNotificationState)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<ulong>>> GetTrackCircuitIdsByDisplayName()
    {
        return await context.TrackCircuits
            .Where(tc => tc.OperationNotificationDisplayName != null)
            .GroupBy(tc => tc.OperationNotificationDisplayName!)
            .ToDictionaryAsync(g => g.Key, g => g.Select(tc => tc.Id).ToList());
    }

    public async Task<List<Models.OperationNotificationState>> GetAllStates()
    {
        // プロセス内キャッシュに載せるので、DbContext の変更追跡には乗せない
        return await context.OperationNotificationStates.AsNoTracking().ToListAsync();
    }

    public async Task SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(DateTime operatedAt)
    {
        await context.OperationNotificationStates
            .Where(s =>
                (s.Type ==  OperationNotificationType.Kaijo || s.Type == OperationNotificationType.Torikeshi) && s.OperatedAt <= operatedAt)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ons => ons.Type, OperationNotificationType.None)
                .SetProperty(ons => ons.Content, string.Empty)
            );
    }
}