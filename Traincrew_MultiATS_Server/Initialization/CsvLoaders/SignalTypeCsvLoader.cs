using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for signal type CSV
/// </summary>
public class SignalTypeCsvLoader(ILogger<SignalTypeCsvLoader> logger) : BaseCsvLoader<SignalTypeCsv>(logger)
{
    public async Task<List<SignalTypeCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/信号何灯式リスト.csv",
            true,
            new SignalTypeCsvMap(),
            cancellationToken);
    }
}
