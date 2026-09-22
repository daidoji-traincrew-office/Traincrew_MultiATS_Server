using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrainSignalState;

public class TrainSignalStateRepository(ApplicationDbContext context) : ITrainSignalStateRepository
{
    public async Task DeleteByTrainNumber(string trainNumber)
    {
        await context.TrainSignalStates
            .Where(tss => tss.TrainNumber == trainNumber)
            .ExecuteDeleteAsync();
    }
}