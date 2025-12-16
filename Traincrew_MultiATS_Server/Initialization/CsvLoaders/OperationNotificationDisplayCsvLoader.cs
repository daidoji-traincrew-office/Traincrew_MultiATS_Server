using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for operation notification display CSV
/// </summary>
public class OperationNotificationDisplayCsvLoader(ILogger<OperationNotificationDisplayCsvLoader> logger)
    : BaseCsvLoader<OperationNotificationDisplayCsv>(logger)
{
    public async Task<List<OperationNotificationDisplayCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.OperationNotificationDisplay,
            false,
            new OperationNotificationDisplayCsvMap(),
            cancellationToken);
    }
}