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
///     Loader for diagram train CSV
/// </summary>
public class DiagramTrainCsvLoader(ILogger<DiagramTrainCsvLoader> logger) : BaseCsvLoader<DiagramTrainCsv>(logger)
{
    public async Task<List<DiagramTrainCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            CsvFilePaths.TrainDiagram,
            true,
            new DiagramTrainCsvMap(),
            cancellationToken);
    }
}