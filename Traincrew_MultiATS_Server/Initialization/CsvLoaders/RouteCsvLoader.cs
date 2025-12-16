using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for route lock track circuit CSV
/// </summary>
public class RouteLockTrackCircuitCsvLoader(ILogger<RouteLockTrackCircuitCsvLoader> logger)
    : BaseCsvLoader<RouteLockTrackCircuitCsv>(logger)
{
    public async Task<List<RouteLockTrackCircuitCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/進路.csv",
            false,
            new RouteLockTrackCircuitCsvMap(),
            cancellationToken);
    }
}