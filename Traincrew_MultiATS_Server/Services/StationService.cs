using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.Services;

public class StationService
{
    private readonly IStationRepository _stationRepository;

    public StationService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public Task<Station?> GetStationById(string id)
    {
        // 駅を取得する
        return _stationRepository.GetStationById(id);
    }

    public Task<Station?> GetStationByName(string name)
    {
        // 駅を取得する
        return _stationRepository.GetStationByName(name);
    }
}