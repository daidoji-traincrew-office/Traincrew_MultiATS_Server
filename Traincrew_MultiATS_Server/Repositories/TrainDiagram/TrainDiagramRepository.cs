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
}
