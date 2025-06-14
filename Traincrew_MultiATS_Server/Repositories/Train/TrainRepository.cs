using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Train;

public class TrainRepository : ITrainRepository
{
    public async Task<TrainState?> GetByNumber(string trainNumber)
    {
        throw new NotImplementedException();
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