using Microsoft.EntityFrameworkCore.Infrastructure;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace WebApplication1.Services;

public class StationService
{
    private readonly IStationRepository _stationRepository;

    public StationService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public Task<Station?> GetStationByName(string name)
    {
        // 駅を取得する
        return _stationRepository.GetStationByName(name);
    }
}