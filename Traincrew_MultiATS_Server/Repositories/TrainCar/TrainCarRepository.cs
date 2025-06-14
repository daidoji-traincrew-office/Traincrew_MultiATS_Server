using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TrainCar;

public class TrainCarRepository : ITrainCarRepository
{
    public async Task<List<TrainCarState>> GetByTrainNumber(string trainNumber)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAll(string trainNumber, List<TrainCarState> carStates)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteByTrainNumber(string trainNumber)
    {
        throw new NotImplementedException();
    }
}
