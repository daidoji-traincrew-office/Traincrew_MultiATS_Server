using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrainDiagram;

public class TrainDiagramRepository(ApplicationDbContext context) : ITrainDiagramRepository
{
    public async Task<Models.TrainDiagram?> GetByTrainNumber(string trainNumber)
    {
        return await context.TrainDiagrams
            .FirstOrDefaultAsync(td => td.TrainNumber == trainNumber);
    }

    public async Task<List<Models.TrainDiagram>> GetByTrainNumbers(ICollection<string> trainNumbers)
    {
        return await context.TrainDiagrams
            .Where(td => trainNumbers.Contains(td.TrainNumber))
            .ToListAsync();
    }

    public async Task<List<string>> GetTrainNumbersForAll(CancellationToken cancellationToken = default)
    {
        return await context.TrainDiagrams
            .Select(td => td.TrainNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, Models.TrainDiagram>> GetForTrainNumberByDiaId(int diaId, CancellationToken cancellationToken = default)
    {
        return await context.TrainDiagrams
            .Where(td => td.DiaId == diaId)
            .ToDictionaryAsync(td => td.TrainNumber, cancellationToken);
    }

    public async Task DeleteTimetablesByDiaId(int diaId, CancellationToken cancellationToken = default)
    {
        await context.TrainDiagramTimetables
            .Where(t => t.TrainDiagram!.DiaId == diaId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
