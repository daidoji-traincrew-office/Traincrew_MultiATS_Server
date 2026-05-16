namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     CSV file paths used by loaders
/// </summary>
public static class CsvFilePaths
{
    /// <summary>
    ///     Base directory for CSV files
    /// </summary>
    private const string DataDirectory = "./Data";

    /// <summary>
    ///     駅・停車場.csv
    /// </summary>
    public const string Station = $"{DataDirectory}/駅・停車場.csv";

    /// <summary>
    ///     軌道回路に対する計算するべき信号機リスト.csv
    /// </summary>
    public const string TrackCircuit = $"{DataDirectory}/軌道回路に対する計算するべき信号機リスト.csv";

    /// <summary>
    ///     信号何灯式リスト.csv
    /// </summary>
    public const string SignalType = $"{DataDirectory}/信号何灯式リスト.csv";

    /// <summary>
    ///     種別.csv
    /// </summary>
    public const string TrainType = $"{DataDirectory}/種別.csv";

    /// <summary>
    ///     列車.csv
    /// </summary>
    public const string TrainDiagram = $"{DataDirectory}/列車.csv";

    /// <summary>
    ///     信号リスト.csv
    /// </summary>
    public const string Signal = $"{DataDirectory}/信号リスト.csv";

    /// <summary>
    ///     運転告知器.csv
    /// </summary>
    public const string OperationNotificationDisplay = $"{DataDirectory}/運転告知器.csv";

    /// <summary>
    ///     進路.csv
    /// </summary>
    public const string RouteLockTrackCircuit = $"{DataDirectory}/進路.csv";

    /// <summary>
    ///     総括制御ペア一覧.csv
    /// </summary>
    public const string ThrowOutControl = $"{DataDirectory}/総括制御ペア一覧.csv";

    /// <summary>
    ///     TTC列番窓.csv
    /// </summary>
    public const string TtcWindow = $"{DataDirectory}/TTC列番窓.csv";

    /// <summary>
    ///     TTC列番窓リンク設定.csv
    /// </summary>
    public const string TtcWindowLink = $"{DataDirectory}/TTC列番窓リンク設定.csv";

    /// <summary>
    ///     RendoTable directory
    /// </summary>
    public const string RendoTableDirectory = $"{DataDirectory}/RendoTable";
}
