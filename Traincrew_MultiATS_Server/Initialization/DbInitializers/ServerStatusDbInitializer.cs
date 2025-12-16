using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes server status entity in the database
/// </summary>
public class ServerStatusDbInitializer(ApplicationDbContext context, ILogger<ServerStatusDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize server status if not already exists
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var serverState = await _context.ServerStates.FirstOrDefaultAsync(cancellationToken);
        if (serverState != null)
        {
            _logger.LogInformation("Server status already initialized");
            return;
        }

        _context.ServerStates.Add(new()
        {
            Mode = ServerMode.Off
        });
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized server status with mode: {Mode}", ServerMode.Off);
    }
}