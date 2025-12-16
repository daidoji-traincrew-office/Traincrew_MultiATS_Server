using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Base class for CSV loaders providing common CSV reading functionality
/// </summary>
public abstract class BaseCsvLoader<T>(ILogger logger)
    where T : class
{
    protected readonly ILogger _logger = logger;

    /// <summary>
    ///     Load CSV data from the specified file path
    /// </summary>
    protected async Task<List<T>> LoadCsvAsync(
        string filePath,
        bool hasHeaderRecord = true,
        ClassMap<T>? csvMap = null,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("CSV file not found: {FilePath}", filePath);
            return [];
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaderRecord
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        if (csvMap != null) csv.Context.RegisterClassMap(csvMap);

        if (!hasHeaderRecord) await csv.ReadAsync();

        var records = await csv.GetRecordsAsync<T>(cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Loaded {Count} records from {FilePath}", records.Count, filePath);
        return records;
    }

    /// <summary>
    ///     Load CSV data synchronously from the specified file path
    /// </summary>
    protected List<T> LoadCsv(
        string filePath,
        bool hasHeaderRecord = true,
        ClassMap<T>? csvMap = null)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("CSV file not found: {FilePath}", filePath);
            return [];
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaderRecord
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        if (csvMap != null) csv.Context.RegisterClassMap(csvMap);

        if (!hasHeaderRecord) csv.Read();

        var records = csv.GetRecords<T>().ToList();

        _logger.LogInformation("Loaded {Count} records from {FilePath}", records.Count, filePath);
        return records;
    }
}