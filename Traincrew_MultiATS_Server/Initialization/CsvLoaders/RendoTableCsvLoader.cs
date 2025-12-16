using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
///     Loader for Rendo Table CSVs (per station)
/// </summary>
public class RendoTableCsvLoader(ILogger<RendoTableCsvLoader> logger)
{
    /// <summary>
    ///     Load all Rendo Table CSV files from the RendoTable directory
    ///     Returns a dictionary mapping station ID to CSV data list
    /// </summary>
    public async Task<Dictionary<string, List<RendoTableCSV>>> LoadAllAsync(
        CancellationToken cancellationToken = default)
    {
        var rendoTableDir = new DirectoryInfo("./Data/RendoTable");
        if (!rendoTableDir.Exists)
        {
            logger.LogWarning("RendoTable directory not found: {Path}", rendoTableDir.FullName);
            return new();
        }

        var csvFiles = rendoTableDir.GetFiles("*.csv");
        logger.LogInformation("Found {Count} Rendo Table CSV files", csvFiles.Length);

        var result = new Dictionary<string, List<RendoTableCSV>>();

        foreach (var file in csvFiles)
        {
            var stationId = Path.GetFileNameWithoutExtension(file.Name);
            var data = await LoadFileAsync(file.FullName, cancellationToken);
            result[stationId] = data;
        }

        return result;
    }

    private async Task<List<RendoTableCSV>> LoadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        // Skip first line (header)
        await csv.ReadAsync();

        var records = new List<RendoTableCSV>();
        await foreach (var record in csv.GetRecordsAsync<RendoTableCSV>(cancellationToken))
        {
            records.Add(record);
        }

        logger.LogInformation("Loaded {Count} records from {FilePath}", records.Count, filePath);
        return records;
    }
}