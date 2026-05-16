using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for TTC window CSV
/// </summary>
public class TtcWindowCsvLoader(ILogger<TtcWindowCsvLoader> logger) : BaseCsvLoader<TtcWindowCsv>(logger)
{
    public virtual async Task<List<TtcWindowCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.TtcWindow,
            false,
            new TtcWindowCsvMap(),
            cancellationToken);
    }
}

/// <summary>
///     Loader for TTC window link CSV
/// </summary>
public class TtcWindowLinkCsvLoader(ILogger<TtcWindowLinkCsvLoader> logger) : BaseCsvLoader<TtcWindowLinkCsv>(logger)
{
    public virtual async Task<List<TtcWindowLinkCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.TtcWindowLink,
            false,
            new TtcWindowLinkCsvMap(),
            cancellationToken);
    }
}