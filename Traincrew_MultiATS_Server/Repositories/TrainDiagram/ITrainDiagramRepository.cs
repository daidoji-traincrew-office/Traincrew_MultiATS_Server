namespace Traincrew_MultiATS_Server.Repositories.TrainDiagram;

public interface ITrainDiagramRepository
{
    Task<Models.TrainDiagram?> GetByTrainNumber(string trainNumber);
}
