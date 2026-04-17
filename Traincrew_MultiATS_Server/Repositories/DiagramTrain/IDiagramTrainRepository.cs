using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.DiagramTrain;

public interface IDiagramTrainRepository
{
    Task<Models.DiagramTrain?> GetByTrainNumber(string trainNumber);
    /// <summary>
    /// ダイヤID、列車番号、駅IDから時刻表を取得する
    /// </summary>
    /// <param name="diaId">ダイヤID</param>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="stationId">駅ID</param>
    /// <returns>時刻表情報</returns>
    Task<DiagramTrainTimetable?> GetTimetableByTrainNumberStationIdAndDiaId(ulong diaId, string trainNumber, string stationId);
    Task<List<Models.DiagramTrain>> GetByTrainNumbers(ICollection<string> trainNumbers);
    Task<List<string>> GetTrainNumbersForAll(CancellationToken cancellationToken = default);
    Task<Dictionary<string, Models.DiagramTrain>> GetForTrainNumberByDiaId(ulong diaId, CancellationToken cancellationToken = default);
    Task DeleteTimetablesByDiaId(ulong diaId, CancellationToken cancellationToken = default);
}
