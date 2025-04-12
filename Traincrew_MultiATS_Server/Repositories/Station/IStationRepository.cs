using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Station;

public interface IStationRepository
{
    Task<Models.Station?> GetStationById(string id);
    Task<Models.Station?> GetStationByName(string name);
    Task<List<Models.Station>> GetStationByIds(IEnumerable<string> ids);
    Task<List<StationTimerState>> GetAllTimerStates();
    Task<List<StationTimerState>> GetTimerStatesByStationIds(IEnumerable<string> stationIds);
}