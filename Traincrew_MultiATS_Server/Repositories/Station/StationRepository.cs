
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Station; 

public class StationRepository(ApplicationDbContext context) : IStationRepository
{
    public Task<Models.Station?> GetStationByName(string name)
    {
        return context.Stations.FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task Save(Models.Station station)
    {
        await context.Stations.AddAsync(station);
    }
}