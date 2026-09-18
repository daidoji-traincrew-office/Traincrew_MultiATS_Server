using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public class OperationNotificationRepository(ApplicationDbContext context) : IOperationNotificationRepository
{
    public async Task<List<Models.OperationNotificationState>> GetAllStates()
    {
        return await context.OperationNotificationStates
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(DateTime operatedAt)
    {
        return await context.OperationNotificationStates
            .Where(s =>
                (s.Type ==  OperationNotificationType.Kaijo || s.Type == OperationNotificationType.Torikeshi) && s.OperatedAt <= operatedAt)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ons => ons.Type, OperationNotificationType.None)
                .SetProperty(ons => ons.Content, string.Empty)
            );
    }
}