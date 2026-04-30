using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.DiagramTrain;

public class DiagramTrainRepository(ApplicationDbContext context) : IDiagramTrainRepository
{
    public async Task<Models.DiagramTrain?> GetByDiaIdAndTrainNumber(ulong diaId, string trainNumber)
    {
        return await context.DiagramTrains
            .FirstOrDefaultAsync(dt => dt.DiaId == diaId && dt.TrainNumber == trainNumber);
    }

    public async Task<DiagramTrainTimetable?> GetTimetableByTrainNumberStationIdAndDiaId(ulong diaId, string trainNumber, string stationId)
    {
        return await context.DiagramTrainTimetables
            .Include(tdt => tdt.TrainDiagram)
            .Where(tdt => tdt.TrainDiagram!.DiaId == diaId && tdt.TrainDiagram.TrainNumber == trainNumber && tdt.StationId == stationId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Models.DiagramTrain>> GetByDiaIdAndTrainNumbers(ulong diaId, ICollection<string> trainNumbers)
    {
        return await context.DiagramTrains
            .Where(dt => dt.DiaId == diaId && trainNumbers.Contains(dt.TrainNumber))
            .ToListAsync();
    }

    public async Task<List<string>> GetTrainNumbersForAll(CancellationToken cancellationToken = default)
    {
        return await context.DiagramTrains
            .Select(dt => dt.TrainNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, Models.DiagramTrain>> GetForTrainNumberByDiaId(ulong diaId, CancellationToken cancellationToken = default)
    {
        return await context.DiagramTrains
            .Where(dt => dt.DiaId == diaId)
            .ToDictionaryAsync(dt => dt.TrainNumber, cancellationToken);
    }

    public async Task DeleteTimetablesByDiaId(ulong diaId, CancellationToken cancellationToken = default)
    {
        await context.DiagramTrainTimetables
            .Where(t => t.TrainDiagram!.DiaId == diaId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
