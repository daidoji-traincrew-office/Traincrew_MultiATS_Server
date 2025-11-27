using System.Reactive;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Route;
using Route = Traincrew_MultiATS_Server.Models.Route;
using RouteData = Traincrew_MultiATS_Server.Common.Models.RouteData;

namespace Traincrew_MultiATS_Server.Services;

using Route = Route;
using RouteData = RouteData;

public class RouteService(IRouteRepository routeRepository)
{
    private static RouteData ToRouteData(Route route)
    {
        return new()
        {
            TcName = route.TcName,
            RouteType = route.RouteType,
            RootId = route.RootId,
            Indicator = route.Indicator,
            ApproachLockTime = route.ApproachLockTime,
            RouteState = new()
            {
                IsRouteRelayRaised = route.RouteState.IsRouteRelayRaised,
                IsSignalControlRaised = route.RouteState.IsSignalControlRaised,
                IsApproachLockMRRaised = route.RouteState.IsApproachLockMRRaised,
                IsApproachLockMSRaised = route.RouteState.IsApproachLockMSRaised,
                IsRouteLockRaised = route.RouteState.IsRouteLockRaised,
                IsLeverRelayRaised = route.RouteState.IsLeverRelayRaised,
                IsThrowOutXRRelayRaised = route.RouteState.IsThrowOutXRRelayRaised,
                IsThrowOutYSRelayRaised = route.RouteState.IsThrowOutYSRelayRaised,
            }
        };
    }

    public async Task<List<RouteData>> GetActiveRoutes()
    {
        var routeIds = await routeRepository.GetIdsWhereRouteRelayWithoutSwitchingMachineIsRaised();
        var routes = await routeRepository.GetByIdsWithState(routeIds);
        return routes.Select(ToRouteData).ToList();
    }

    public async Task<List<RouteData>> GetAllRoutes()
    {
        var routeIds = await routeRepository.GetIdsForAll();
        var routes = await routeRepository.GetByIdsWithState(routeIds);
        return routes.Select(ToRouteData).ToList();
    }
}