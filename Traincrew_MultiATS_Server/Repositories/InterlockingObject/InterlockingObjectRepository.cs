﻿using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.InterlockingObject;

public class InterlockingObjectRepository(ApplicationDbContext context) : IInterlockingObjectRepository
{
    public Task<List<Models.InterlockingObject>> GetAllWithState()
    {
        return context.InterlockingObjects
            .Include(obj => ((Models.Route)obj).RouteState)
            .Include(obj => ((Models.SwitchingMachine)obj).SwitchingMachineState)
            .Include(obj => ((Models.TrackCircuit)obj).TrackCircuitState)
            .Include(obj => ((Models.Lever)obj).LeverState)
            .Include(obj => ((Models.DirectionRoute)obj).DirectionRouteState)
            .Include(obj => ((Models.DirectionSelfControlLever)obj).DirectionSelfControlLeverState)
            .ToListAsync();
    }
    public Task<List<Models.InterlockingObject>> GetObjectByIds(IEnumerable<ulong> ids)
    {
        // Todo: 渡されたIDのオブジェクトが、かならず存在することを保証する
        return context.InterlockingObjects
            .Where(obj => ids.Contains(obj.Id))
            .ToListAsync();
    }
    public Task<List<Models.InterlockingObject>> GetObjectByIdsWithState(IEnumerable<ulong> ids)
    {
        // Todo: 渡されたIDのオブジェクトが、かならず存在することを保証する
        return context.InterlockingObjects
            .Where(obj => ids.Contains(obj.Id))
            .Include(obj => ((Models.Route)obj).RouteState)
            .Include(obj => ((Models.SwitchingMachine)obj).SwitchingMachineState)
            .Include(obj => ((Models.TrackCircuit)obj).TrackCircuitState)
            .Include(obj => ((Models.Lever)obj).LeverState)
            .Include(obj => ((Models.DirectionRoute)obj).DirectionRouteState)
            .Include(obj => ((Models.DirectionSelfControlLever)obj).DirectionSelfControlLeverState)
            .ToListAsync();
    }
    public Task<Models.InterlockingObject> GetObject(string name)
    {
        return context.InterlockingObjects
            .Where(obj => obj.Name == name)
            .FirstAsync();
    }

    public async Task<List<Models.InterlockingObject>> GetObjectsByStationIdsWithState(List<string> stationIds)
    {
        return await context.InterlockingObjects
            .Where(obj => stationIds.Any(stationId => obj.Name.Contains(stationId)))
            .Include(obj => ((Models.Route)obj).RouteState)
            .Include(obj => ((Models.SwitchingMachine)obj).SwitchingMachineState)
            .Include(obj => ((Models.TrackCircuit)obj).TrackCircuitState)
            .Include(obj => ((Models.Lever)obj).LeverState)
            .Include(obj => ((Models.DirectionRoute)obj).DirectionRouteState)
            .Include(obj => ((Models.DirectionSelfControlLever)obj).DirectionSelfControlLeverState)
            .ToListAsync();
    }
}