using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for infrastructure CSV files (stations, track circuits, signal types)
/// </summary>
public class InfrastructureCsvLoader(ILogger<InfrastructureCsvLoader> logger)
{
    /// <summary>
    ///     Load infrastructure CSV data (stations, track circuits, signal types)
    ///     Returns null if any required file is missing
    /// </summary>
    public async Task<InfrastructureCsvData?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var stationFile = new FileInfo("./Data/駅・停車場.csv");
        var trackCircuitFile = new FileInfo("./Data/軌道回路に対する計算するべき信号機リスト.csv");
        var signalTypeFile = new FileInfo("./Data/信号何灯式リスト.csv");

        if (!stationFile.Exists || !trackCircuitFile.Exists || !signalTypeFile.Exists)
        {
            logger.LogWarning(
                "Infrastructure CSV files not found. Station: {StationExists}, TrackCircuit: {TrackCircuitExists}, SignalType: {SignalTypeExists}",
                stationFile.Exists,
                trackCircuitFile.Exists,
                signalTypeFile.Exists);
            return null;
        }

        var stationLoader = new StationCsvLoader(logger);
        var trackCircuitLoader = new TrackCircuitCsvLoader(logger);
        var signalTypeLoader = new SignalTypeCsvLoader(logger);

        var stationList = await stationLoader.LoadAsync(cancellationToken);
        var trackCircuitList = await trackCircuitLoader.LoadAsync(cancellationToken);
        var signalTypeList = await signalTypeLoader.LoadAsync(cancellationToken);

        return new(stationList, trackCircuitList, signalTypeList);
    }
}

/// <summary>
///     Container for infrastructure CSV data
/// </summary>
public record InfrastructureCsvData(
    List<StationCsv> StationList,
    List<TrackCircuitCsv> TrackCircuitList,
    List<SignalTypeCsv> SignalTypeList);

/// <summary>
///     Loader for station CSV
/// </summary>
public class StationCsvLoader(ILogger logger) : BaseCsvLoader<StationCsv>(logger)
{
    public async Task<List<StationCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/駅・停車場.csv",
            true,
            new StationCsvMap(),
            cancellationToken);
    }
}

/// <summary>
///     Loader for track circuit CSV
/// </summary>
public class TrackCircuitCsvLoader(ILogger logger) : BaseCsvLoader<TrackCircuitCsv>(logger)
{
    public async Task<List<TrackCircuitCsv>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await LoadCsvAsync(
            "./Data/軌道回路に対する計算するべき信号機リスト.csv",
            true,
            new TrackCircuitCsvMap(),
            cancellationToken);
    }
}

/// <summary>
///     Loader for signal type CSV
/// </summary>
public class SignalTypeCsvLoader(ILogger logger) : BaseCsvLoader<SignalTypeCsv>(logger)
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