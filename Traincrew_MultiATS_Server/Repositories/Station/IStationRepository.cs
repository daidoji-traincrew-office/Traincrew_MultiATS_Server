using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Station;

public interface IStationRepository
{
    /// <summary>
    /// 停車場のみを取得する
    /// </summary>
    /// <returns></returns>
    Task<List<Models.Station>> GetWhereIsStation();
    Task<Models.Station?> GetStationById(string id);
    Task<Models.Station?> GetStationByName(string name);
    Task<List<Models.Station>> GetStationByIds(IEnumerable<string> ids);
    Task<List<StationTimerState>> GetAllTimerStates();
    Task<List<StationTimerState>> GetTimerStatesByStationIds(IEnumerable<string> stationIds);
}