using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for track circuit CSV
/// </summary>
public class TrackCircuitCsvLoader(ILogger<TrackCircuitCsvLoader> logger) : BaseCsvLoader<TrackCircuitCsv>(logger)
{
    public async Task<List<TrackCircuitCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.TrackCircuit,
            true,
            new TrackCircuitCsvMap(),
            cancellationToken);
    }
}
