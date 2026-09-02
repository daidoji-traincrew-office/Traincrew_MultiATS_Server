using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

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
    public async Task<List<Models.TtcWindow>> GetTtcWindowsByStationIdsWithState(List<string> stationIds)
    {
        //所属駅の他に表示駅も含めるため以下のようにする
        //①TtcWindow.StationIdがstationIdsに含まれるTtcWindowを取得
        //②TtcWindowDisplayStation.StationIdがstationIdsに含まれるTtcWindowを取得

        var ttcWindows = await context.TtcWindows
            .Include(t => t.TtcWindowState)
            .Where(t => stationIds.Contains(t.StationId))
            .ToListAsync();
        var ttcWindowNames = await context.TtcWindowDisplayStations
            .Where(t => stationIds.Contains(t.StationId))
            .ToListAsync();
        var ttcWindowNamesList = ttcWindowNames.Select(t => t.TtcWindowName).ToList();

        var ttcWindows2 = await context.TtcWindows
            .Include(t => t.TtcWindowState)
            .Where(t => ttcWindowNamesList.Contains(t.Name))
            .ToListAsync();
        //TtcWindowとttcWindows2の結合
        ttcWindows.AddRange(ttcWindows2);
        //重複を排除
        var distinctTtcWindows = ttcWindows
            .GroupBy(t => t.Name)
            .Select(g => g.First())
            .ToList();
        return distinctTtcWindows;
    }
    public async Task<List<Models.TtcWindow>> GetTtcWindowsByTrainNumber(string trainNumber)
    {
        return await context.TtcWindows
            .Include(t => t.TtcWindowState)
            .Where(t => t.TtcWindowState.TrainNumber == trainNumber)
            .ToListAsync();
    }

    public async Task<List<string>> GetAllWindowNamesAsync(CancellationToken cancellationToken = default)
    {
        return await context.TtcWindows
            .Select(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
