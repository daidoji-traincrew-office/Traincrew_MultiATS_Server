using System.Collections;
using System.Text.Json;

namespace Traincrew_MultiATS_Server.Initialization.JsonLoaders;

/// <summary>
///     Base class for JSON loaders providing common JSON reading functionality
/// </summary>
public abstract class BaseJsonLoader<T>(ILogger logger)
    where T : class
{
    /// <summary>
    ///     Load JSON data from the specified file path
    /// </summary>
    protected async Task<T?> LoadJsonAsync(
        string filePath,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            logger.LogWarning("JSON file not found: {FilePath}", filePath);
            return null;
        }

        await using var stream = fileInfo.OpenRead();
        var data = await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);

        if (data == null)
        {
            logger.LogWarning("Failed to deserialize JSON from {FilePath}", filePath);
            return null;
        }

        // Log record count for collections
        if (data is ICollection collection)
        {
            logger.LogInformation("Loaded {Count} records from {FilePath}", collection.Count, filePath);
        }
        else
        {
            logger.LogInformation("Successfully loaded JSON from {FilePath}", filePath);
        }

        return data;
    }
}
