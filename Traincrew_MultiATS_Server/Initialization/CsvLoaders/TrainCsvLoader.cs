using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for train type CSV
/// </summary>
public class TrainTypeCsvLoader(ILogger<TrainTypeCsvLoader> logger) : BaseCsvLoader<TrainTypeCsv>(logger)
{
    public async Task<List<TrainTypeCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.TrainType,
            true,
            new TrainTypeCsvMap(),
            cancellationToken);
    }
}

/// <summary>
///     Loader for train diagram CSV
/// </summary>
public class TrainDiagramCsvLoader(ILogger<TrainDiagramCsvLoader> logger) : BaseCsvLoader<TrainDiagramCsv>(logger)
{
    public async Task<List<TrainDiagramCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.TrainDiagram,
            true,
            new TrainDiagramCsvMap(),
            cancellationToken);
    }
}