using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Train;

public class TrainRepository(ApplicationDbContext context) : ITrainRepository
{
    public async Task<List<TrainState>> GetByDiaNumber(int diaNumber)
    {
        return await context.TrainStates
            .Where(ts => ts.DiaNumber == diaNumber)
            .ToListAsync();
    }

    public async Task<List<TrainState>> GetByTrainNumbers(ICollection<string> trainNumbers)
    {
        return await context.TrainStates
            .Where(ts => trainNumbers.Contains(ts.TrainNumber))
            .ToListAsync();
    }

    public async Task<TrainState?> GetByDriverId(ulong driverId)
    {
        return await context.TrainStates
            .FirstOrDefaultAsync(ts => driverId == ts.DriverId);
    }

    public async Task DeleteByTrainNumber(string trainNumber)
    {
        await context.TrainStates
            .Where(ts => ts.TrainNumber == trainNumber)
            .ExecuteDeleteAsync();
    }

    public async Task Create(TrainState trainState)
    {
        context.TrainStates.Add(trainState);
        await context.SaveChangesAsync();
    }

    public async Task Update(TrainState trainState)
    {
        context.TrainStates.Update(trainState);
        await context.SaveChangesAsync();
    }

    public async Task<List<TrainState>> GetAll()
    {
        return await context.TrainStates.ToListAsync();
    }

    public async Task<TrainState?> GetById(long id)
    {
        return await context.TrainStates
            .FirstOrDefaultAsync(ts => ts.Id == id);
    }
}