using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Server;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes server status entity in the database
/// </summary>
public class ServerStatusDbInitializer(
    ILogger<ServerStatusDbInitializer> logger,
    IServerRepository serverRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer(logger)
{
    /// <summary>
    ///     Initialize server status if not already exists
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var serverState = await serverRepository.GetServerStateAsync();
        if (serverState != null)
        {
            _logger.LogInformation("Server status already initialized");
            return;
        }

        await serverRepository.AddServerStateAsync(new ServerState
        {
            Mode = ServerMode.Off
        }, cancellationToken);
        _logger.LogInformation("Initialized server status with mode: {Mode}", ServerMode.Off);
    }
}