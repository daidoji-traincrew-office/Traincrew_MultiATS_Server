namespace Traincrew_MultiATS_Server.Repositories.TrainType;

public interface ITrainTypeRepository
{
    Task<List<long>> GetIdsForAll(CancellationToken cancellationToken = default);
}
