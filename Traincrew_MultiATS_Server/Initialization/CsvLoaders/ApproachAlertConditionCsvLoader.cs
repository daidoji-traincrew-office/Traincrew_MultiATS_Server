using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

public class ApproachAlertConditionCsvLoader(ILogger<ApproachAlertConditionCsvLoader> logger)
    : BaseCsvLoader<ApproachAlertConditionCsv>(logger)
{
    public async Task<List<ApproachAlertConditionCsv>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.ApproachAlertCondition,
            true,
            null,
            cancellationToken);
    }
}
