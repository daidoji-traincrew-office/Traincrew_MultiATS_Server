using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.OperationNotificationDisplay;

public class OperationNotificationDisplayRepository(ApplicationDbContext context) : IOperationNotificationDisplayRepository
{
    public async Task<List<string>> GetAllNames(CancellationToken cancellationToken = default)
    {
        return await context.OperationNotificationDisplays
            .Select(ond => ond.Name)
            .ToListAsync(cancellationToken);
    }
}
