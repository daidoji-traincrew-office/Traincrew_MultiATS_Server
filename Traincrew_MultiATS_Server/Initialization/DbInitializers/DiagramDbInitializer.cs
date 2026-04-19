using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Diagram;
using Traincrew_MultiATS_Server.Repositories.General;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

public class DiagramDbInitializer(
    ILogger<DiagramDbInitializer> logger,
    IDiagramRepository diagramRepository,
    IGeneralRepository generalRepository,
    TrainDbInitializer trainDbInitializer)
{
    public async Task InitializeAsync(List<DiagramJson> diagramJsonList, CancellationToken cancellationToken = default)
    {
        var existingDiagrams = await diagramRepository.GetAllForNameAndTimeRange(cancellationToken);

        foreach (var diagramJson in diagramJsonList)
        {
            var key = (diagramJson.Name, diagramJson.TimeRange);
            ulong diaId;

            if (existingDiagrams.TryGetValue(key, out var existing))
            {
                if (existing.Version != diagramJson.Version || existing.Index != diagramJson.Index)
                {
                    existing.Version = diagramJson.Version;
                    existing.Index = diagramJson.Index;
                    await generalRepository.Save(existing, cancellationToken);
                    logger.LogInformation("Updated diagram version: {Name} {TimeRange} → {Version}",
                        diagramJson.Name, diagramJson.TimeRange, diagramJson.Version);
                }

                diaId = existing.Id;
            }
            else
            {
                var newDiagram = new Diagram
                {
                    Name = diagramJson.Name,
                    TimeRange = diagramJson.TimeRange,
                    Version = diagramJson.Version,
                    Index = diagramJson.Index
                };
                await generalRepository.Add(newDiagram, cancellationToken);
                diaId = newDiagram.Id;
                logger.LogInformation("Added new diagram: {Name} {TimeRange}", diagramJson.Name, diagramJson.TimeRange);
            }

            var ttcData = new TTC_Data { TrainList = diagramJson.TrainList };
            await trainDbInitializer.InitializeFromTtcDataAsync(ttcData, diaId, cancellationToken);
        }

        logger.LogInformation("Diagram initialization completed: {Count} diagrams processed", diagramJsonList.Count);
    }
}
