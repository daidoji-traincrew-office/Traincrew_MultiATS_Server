using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.SignalType;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes signal type entities in the database
/// </summary>
public class SignalTypeDbInitializer(
    ILogger<SignalTypeDbInitializer> logger,
    ISignalTypeRepository signalTypeRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer(logger)
{
    /// <summary>
    ///     Initialize signal types from CSV data
    /// </summary>
    public async Task InitializeSignalTypesAsync(List<SignalTypeCsv> signalTypeList,
        CancellationToken cancellationToken = default)
    {
        var signalTypeNames = await signalTypeRepository.GetAllNames(cancellationToken);

        var signalTypes = new List<Models.SignalType>();
        foreach (var signalTypeData in signalTypeList)
        {
            if (signalTypeNames.Contains(signalTypeData.Name))
            {
                continue;
            }

            signalTypes.Add(new()
            {
                Name = signalTypeData.Name,
                RIndication = GetSignalIndication(signalTypeData.RIndication),
                YYIndication = GetSignalIndication(signalTypeData.YYIndication),
                YIndication = GetSignalIndication(signalTypeData.YIndication),
                YGIndication = GetSignalIndication(signalTypeData.YGIndication),
                GIndication = GetSignalIndication(signalTypeData.GIndication)
            });
        }

        await generalRepository.AddAll(signalTypes);
        _logger.LogInformation("Initialized {Count} signal types", signalTypes.Count);
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