using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TtcWindow;

public class TtcWindowRepository(ApplicationDbContext context) : ITtcWindowRepository
{
    public async Task<List<Models.TtcWindow>> GetAllTtcWindowWithState()
    {
        return await context.TtcWindows
            .Include(t => t.TtcWindowState)
            .ToListAsync();
    }
    public async Task<List<Models.TtcWindow>> GetTtcWindowWithStateByName(List<string> ttcWindowNames)
    {
        return await context.TtcWindows
            .Include(t => t.TtcWindowState)
            .Where(t => ttcWindowNames.Contains(t.Name))
            .ToListAsync();
    }
    public async Task<List<Models.TtcWindowTrackCircuit>> GetWindowTrackCircuits()
    {
        return await context.TtcWindowTrackCircuits
            .ToListAsync();
    }
    public async Task<List<Models.TtcWindowTrackCircuit>> ttcWindowTrackCircuitsById(List<string> ttcWindowName)
    {
        return await context.TtcWindowTrackCircuits
            .Where(obj => ttcWindowName.Contains(obj.TtcWindowName))
            .ToListAsync();
    }
}
