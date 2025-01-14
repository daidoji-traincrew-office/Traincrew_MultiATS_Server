using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;


namespace Traincrew_MultiATS_Server.Repositories.TrackCircuit;

public class TrackCircuitRepository(ApplicationDbContext context) : ITrackCircuitRepository
{
    public async Task<List<Models.TrackCircuit>> GetAllTrackCircuitList()
    {
        List<Models.TrackCircuit> trackcircuitlist_db = await context.TrackCircuits.ToListAsync();
		return trackcircuitlist_db;
    }
}