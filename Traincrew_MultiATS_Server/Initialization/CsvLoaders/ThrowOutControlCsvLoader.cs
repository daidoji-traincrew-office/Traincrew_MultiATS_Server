using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for throw out control CSV
/// </summary>
public class ThrowOutControlCsvLoader(ILogger<ThrowOutControlCsvLoader> logger)
    : BaseCsvLoader<ThrowOutControlCsv>(logger)
{
    public async Task<List<ThrowOutControlCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/総括制御ペア一覧.csv",
            false,
            new ThrowOutControlCsvMap(),
            cancellationToken);
    }
}