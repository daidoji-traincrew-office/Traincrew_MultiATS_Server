using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.Services;

public class StationService(IStationRepository stationRepository)
{
    public Task<Station?> GetStationById(string id)
    {
        // 駅を取得する
        return stationRepository.GetStationById(id);
    }

    public Task<Station?> GetStationByName(string name)
    {
        // 駅を取得する
        return stationRepository.GetStationByName(name);
    }

    public async Task<string?> GetStationNameById(string id)
    {
        var station = await stationRepository.GetStationById(id);
        return station?.Name;
    }

    public async Task<List<string>> GetStationNamesByIds(List<string> ids)
    {
        var stations = await stationRepository.GetStationByIds(ids);
        return stations.Select(s => s.Name).ToList();
    }
}
