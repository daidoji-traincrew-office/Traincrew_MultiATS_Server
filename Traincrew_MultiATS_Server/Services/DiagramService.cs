using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Diagram;

namespace Traincrew_MultiATS_Server.Services;

public interface IDiagramService
{
    Task<List<DiagramData>> GetAllDiagramsAsync();
}

public class DiagramService(IDiagramRepository diagramRepository) : IDiagramService
{
    public async Task<List<DiagramData>> GetAllDiagramsAsync()
    {
        var diagrams = await diagramRepository.GetAll();
        return [..diagrams.Select(d => new DiagramData
        {
            Id = d.Id,
            Name = d.Name,
            TimeRange = d.TimeRange
        })];
    }
}
