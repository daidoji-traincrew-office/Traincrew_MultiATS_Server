using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TtcWindow;

public class TtcWindowRepository(ApplicationDbContext context) : ITtcWindowRepository
{
    public async Task<List<Models.TtcWindow>> GetAllTtcWindow()
    {
        return await context.TtcWindows
            .ToListAsync();
    }
    public async Task<List<Models.TtcWindow>> GetTtcWindowByName(List<string> ttcWindowNames)
    {
        return await context.TtcWindows
            .Where(obj => ttcWindowNames.Contains(obj.Name))
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
