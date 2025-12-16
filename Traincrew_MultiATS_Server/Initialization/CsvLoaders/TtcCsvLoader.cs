using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for TTC window CSV
/// </summary>
public class TtcWindowCsvLoader(ILogger<TtcWindowCsvLoader> logger) : BaseCsvLoader<TtcWindowCsv>(logger)
{
    public async Task<List<TtcWindowCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/TTC列番窓.csv",
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
    public async Task<List<TtcWindowLinkCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/TTC列番窓リンク設定.csv",
            false,
            new TtcWindowLinkCsvMap(),
            cancellationToken);
    }
}