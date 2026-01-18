namespace Traincrew_MultiATS_Server.Repositories.TrainDiagram;

public interface ITrainDiagramRepository
{
    Task<Models.TrainDiagram?> GetByTrainNumber(string trainNumber);
    Task<List<Models.TrainDiagram>> GetByTrainNumbers(ICollection<string> trainNumbers);
    Task<List<string>> GetTrainNumbersForAll(CancellationToken cancellationToken = default);
    Task<Dictionary<string, Models.TrainDiagram>> GetForTrainNumberByDiaId(int diaId, CancellationToken cancellationToken = default);
    Task DeleteTimetablesByDiaId(int diaId, CancellationToken cancellationToken = default);
}
