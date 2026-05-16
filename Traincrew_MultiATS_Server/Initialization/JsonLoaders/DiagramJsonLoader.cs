using System.Text.Json;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.JsonLoaders;

public class DiagramJsonLoader(ILogger<DiagramJsonLoader> logger) : BaseJsonLoader<DiagramJson>(logger)
{
    private const string DiagramDirectory = "./Data/Diagram";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    public async Task<List<DiagramJson>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DiagramJson>();

        var files = Directory.EnumerateFiles(DiagramDirectory, "*.json", SearchOption.AllDirectories);
        foreach (var filePath in files)
        {
            var data = await LoadJsonAsync(filePath, Options, cancellationToken);
            if (data != null)
            {
                results.Add(data);
            }
        }

        logger.LogInformation("Loaded {Count} diagram JSON files from {Directory}", results.Count, DiagramDirectory);
        return results;
    }
}
