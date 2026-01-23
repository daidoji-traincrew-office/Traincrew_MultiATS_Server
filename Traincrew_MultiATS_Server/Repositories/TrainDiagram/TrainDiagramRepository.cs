using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TrainDiagram;

public class TrainDiagramRepository(ApplicationDbContext context) : ITrainDiagramRepository
{
    public async Task<Models.TrainDiagram?> GetByTrainNumber(string trainNumber)
    {
        return await context.TrainDiagrams
            .FirstOrDefaultAsync(td => td.TrainNumber == trainNumber);
    }

    public async Task<TrainDiagramTimetable?> GetTimetableByTrainNumberStationIdAndDiaId(int diaId, string trainNumber, string stationId)
    {
        return await context.TrainDiagramTimetables
            .Include(tdt => tdt.TrainDiagram)
            .Where(tdt => tdt.TrainDiagram!.DiaId == diaId && tdt.TrainDiagram.TrainNumber == trainNumber && tdt.StationId == stationId)
            .FirstOrDefaultAsync();
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
