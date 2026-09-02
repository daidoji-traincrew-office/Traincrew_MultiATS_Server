using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for signal CSV
/// </summary>
public class SignalCsvLoader(ILogger<SignalCsvLoader> logger) : BaseCsvLoader<SignalCsv>(logger)
{
    public List<SignalCsv> Load()
    {
        return LoadCsv(
            CsvFilePaths.Signal,
            true,
            new SignalCsvMap());
    }
}