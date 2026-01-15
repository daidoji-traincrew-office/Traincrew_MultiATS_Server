namespace Traincrew_MultiATS_Server.Repositories.TrainDiagram;

public interface ITrainDiagramRepository
{
    Task<Models.TrainDiagram?> GetByTrainNumber(string trainNumber);
    Task<List<Models.TrainDiagram>> GetByTrainNumbers(ICollection<string> trainNumbers);
    Task<List<string>> GetTrainNumbersForAll(CancellationToken cancellationToken = default);
    Task AddAsync(Models.TrainDiagram trainDiagram, CancellationToken cancellationToken = default);
}
