using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Train;

public class TrainRepository(ApplicationDbContext context) : ITrainRepository
{
    public async Task<TrainState?> GetByDiaNumber(int diaNumber)
    {
        return await context.TrainStates
            .FirstOrDefaultAsync(ts => ts.DiaNumber == diaNumber);
    }

    public async Task<List<TrainState>> GetByTrainNumbers(ICollection<string> trainNumbers)
    {
        return await context.TrainStates
            .Where(ts => trainNumbers.Contains(ts.TrainNumber))
            .ToListAsync();
    }

    public async Task DeleteByTrainNumber(string trainNumber)
    {
        throw new NotImplementedException();
    }

    public async Task Create(TrainState trainState)
    {
        throw new NotImplementedException();
    }

    public async Task Update(TrainState trainState)
    {
        throw new NotImplementedException();
    }
}