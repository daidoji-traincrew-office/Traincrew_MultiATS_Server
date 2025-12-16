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
            "./Data/軌道回路に対する計算するべき信号機リスト.csv",
            true,
            new TrackCircuitCsvMap(),
            cancellationToken);
    }
}
