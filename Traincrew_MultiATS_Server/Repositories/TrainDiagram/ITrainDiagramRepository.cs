using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TrainDiagram;

public interface ITrainDiagramRepository
{
    Task<Models.TrainDiagram?> GetByTrainNumber(string trainNumber);
    /// <summary>
    /// ダイヤID、列車番号、駅IDから時刻表を取得する
    /// </summary>
    /// <param name="diaId">ダイヤID</param>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="stationId">駅ID</param>
    /// <returns>時刻表情報</returns>
    Task<TrainDiagramTimetable?> GetTimetableByTrainNumberStationIdAndDiaId(int diaId, string trainNumber, string stationId);
    Task<List<Models.TrainDiagram>> GetByTrainNumbers(ICollection<string> trainNumbers);
    Task<List<string>> GetTrainNumbersForAll(CancellationToken cancellationToken = default);
    Task<Dictionary<string, Models.TrainDiagram>> GetForTrainNumberByDiaId(int diaId, CancellationToken cancellationToken = default);
    Task DeleteTimetablesByDiaId(int diaId, CancellationToken cancellationToken = default);
}
