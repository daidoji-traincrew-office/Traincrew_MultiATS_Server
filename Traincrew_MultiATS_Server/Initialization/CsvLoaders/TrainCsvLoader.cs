using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for train type CSV
/// </summary>
public class TrainTypeCsvLoader(ILogger<TrainTypeCsvLoader> logger) : BaseCsvLoader<TrainTypeCsv>(logger)
{
    public List<TrainTypeCsv> Load()
    {
        return LoadCsv(
            "./Data/種別.csv",
            true,
            new TrainTypeCsvMap());
    }
}

/// <summary>
///     Loader for train diagram CSV
/// </summary>
public class TrainDiagramCsvLoader(ILogger<TrainDiagramCsvLoader> logger) : BaseCsvLoader<TrainDiagramCsv>(logger)
{
    public List<TrainDiagramCsv> Load()
    {
        return LoadCsv(
            "./Data/列車.csv",
            true,
            new TrainDiagramCsvMap());
    }
}