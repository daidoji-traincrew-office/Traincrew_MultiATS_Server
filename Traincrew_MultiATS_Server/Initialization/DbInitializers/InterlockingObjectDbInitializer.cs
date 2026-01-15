using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes interlocking objects by setting station IDs based on naming patterns
/// </summary>
public partial class InterlockingObjectDbInitializer(
    ILogger<InterlockingObjectDbInitializer> logger,
    IInterlockingObjectRepository interlockingObjectRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer(logger)
{
    [GeneratedRegex(@"^(TH(\d{1,2}S?))_")]
    private static partial Regex RegexStationId();

    /// <summary>
    ///     Initialize interlocking objects by setting station IDs based on naming pattern
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var interlockingObjects = await interlockingObjectRepository.GetAllAsync(cancellationToken);

        var updatedObjects = new List<InterlockingObject>();
        foreach (var interlockingObject in interlockingObjects)
        {
            var match = RegexStationId().Match(interlockingObject.Name);
            if (!match.Success)
            {
                continue;
            }

            var stationId = match.Groups[1].Value;
            interlockingObject.StationId = stationId;
            updatedObjects.Add(interlockingObject);
        }

        await generalRepository.SaveAll(updatedObjects, cancellationToken);
        _logger.LogInformation("Set station ID for {Count} interlocking objects", updatedObjects.Count);
    }
}
