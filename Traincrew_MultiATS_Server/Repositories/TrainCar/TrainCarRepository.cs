using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TrainCar;

public class TrainCarRepository : ITrainCarRepository
{
    public async Task<List<TrainCarState>> GetByTrainNumber(string trainNumber)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAll(long trainStateId, List<TrainCarState> carStates)
    {
        // 差分を取って、追加・更新・削除を行う
        throw new NotImplementedException();
    }

    public async Task DeleteByTrainNumber(string trainNumber)
    {
        throw new NotImplementedException();
    }
}
