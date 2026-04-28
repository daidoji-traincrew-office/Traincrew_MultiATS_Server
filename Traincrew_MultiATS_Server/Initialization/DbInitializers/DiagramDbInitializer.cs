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
        var existingDiagrams = await diagramRepository.GetAllForName(cancellationToken);

        foreach (var diagramJson in diagramJsonList)
        {
            ulong diaId;

            if (existingDiagrams.TryGetValue(diagramJson.Name, out var existing))
            {
                if (existing.Version != diagramJson.Version)
                {
                    existing.Version = diagramJson.Version;
                    await generalRepository.Save(existing, cancellationToken);
                    logger.LogInformation("Updated diagram version: {Name} → {Version}",
                        diagramJson.Name, diagramJson.Version);
                }

                diaId = existing.Id;
            }
            else
            {
                var newDiagram = new Diagram
                {
                    Name = diagramJson.Name,
                    Version = diagramJson.Version
                };
                await generalRepository.Add(newDiagram, cancellationToken);
                diaId = newDiagram.Id;
                logger.LogInformation("Added new diagram: {Name}", diagramJson.Name);
            }

            var ttcData = new TTC_Data { TrainList = diagramJson.TrainList };
            await trainDbInitializer.InitializeFromTtcDataAsync(ttcData, diaId, cancellationToken);
        }

        logger.LogInformation("Diagram initialization completed: {Count} diagrams processed", diagramJsonList.Count);
    }
}
