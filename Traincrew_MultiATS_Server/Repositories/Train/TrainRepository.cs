using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Train;

public class TrainRepository : ITrainRepository
{
    public async Task<TrainState?> GetTrainByNumber(string trainNumber)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteTrain(string trainNumber)
    {
        throw new NotImplementedException();
    }
}