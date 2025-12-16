using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes signal type entities in the database
/// </summary>
public class SignalTypeDbInitializer(ApplicationDbContext context, ILogger<SignalTypeDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize signal types from CSV data
    /// </summary>
    public async Task InitializeSignalTypesAsync(List<SignalTypeCsv> signalTypeList,
        CancellationToken cancellationToken = default)
    {
        var signalTypeNames = (await _context.SignalTypes
            .Select(st => st.Name)
            .ToListAsync(cancellationToken)).ToHashSet();

        var addedCount = 0;
        foreach (var signalTypeData in signalTypeList)
        {
            if (signalTypeNames.Contains(signalTypeData.Name)) continue;

            _context.SignalTypes.Add(new()
            {
                Name = signalTypeData.Name,
                RIndication = GetSignalIndication(signalTypeData.RIndication),
                YYIndication = GetSignalIndication(signalTypeData.YYIndication),
                YIndication = GetSignalIndication(signalTypeData.YIndication),
                YGIndication = GetSignalIndication(signalTypeData.YGIndication),
                GIndication = GetSignalIndication(signalTypeData.GIndication)
            });
            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} signal types", addedCount);
    }

    private static SignalIndication GetSignalIndication(string indication)
    {
        return indication switch
        {
            "R" => SignalIndication.R,
            "YY" => SignalIndication.YY,
            "Y" => SignalIndication.Y,
            "YG" => SignalIndication.YG,
            "G" => SignalIndication.G,
            _ => SignalIndication.R
        };
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for SignalTypeDbInitializer as it requires CSV data
        // Use InitializeSignalTypesAsync instead
        await Task.CompletedTask;
    }
}