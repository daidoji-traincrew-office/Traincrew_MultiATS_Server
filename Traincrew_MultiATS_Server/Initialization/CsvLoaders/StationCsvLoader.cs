using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for station CSV
/// </summary>
public class StationCsvLoader(ILogger<StationCsvLoader> logger) : BaseCsvLoader<StationCsv>(logger)
{
    public async Task<List<StationCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/駅・停車場.csv",
            true,
            new StationCsvMap(),
            cancellationToken);
    }
}
