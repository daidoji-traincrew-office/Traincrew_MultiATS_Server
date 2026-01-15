using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.SignalType;

public class SignalTypeRepository(ApplicationDbContext context) : ISignalTypeRepository
{
    public async Task<HashSet<string>> GetAllNames(CancellationToken cancellationToken = default)
    {
        return (await context.SignalTypes
            .Select(st => st.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
    }
}
