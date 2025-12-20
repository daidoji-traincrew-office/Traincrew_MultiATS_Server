using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Base class for database initializers
/// </summary>
public abstract class BaseDbInitializer(ApplicationDbContext context, ILogger logger)
{
    protected readonly ApplicationDbContext _context = context;
    protected readonly ILogger _logger = logger;

    /// <summary>
    ///     Initialize database entities
    /// </summary>
    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Detach unchanged entities to improve performance
    /// </summary>
    protected void DetachUnchangedEntities()
    {
        var unchangedEntries = _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Unchanged)
            .ToList();

        foreach (var entry in unchangedEntries)
        {
            entry.State = EntityState.Detached;
        }
    }
}