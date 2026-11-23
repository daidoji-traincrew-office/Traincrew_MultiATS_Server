using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Route;
using Route = Traincrew_MultiATS_Server.Models.Route;
using RouteData = Traincrew_MultiATS_Server.Common.Models.RouteData;

namespace Traincrew_MultiATS_Server.Services;

using Route = Route;
using RouteData = RouteData;

public interface IRouteService
{
    Task<List<RouteData>> GetActiveRoutes();
    Task<List<RouteData>> GetAllRoutes();

    /// <summary>
    /// 駅IDから進路を状態付きで取得する
    /// </summary>
    Task<List<RouteData>> GetRoutesWithStateByStationIds(List<string> stationIds);
}

public class RouteService(IRouteRepository routeRepository) : IRouteService
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
            RouteState = route.RouteState == null
                ? null
                : new RouteStateData
                {
                    IsSignalControlRaised = route.RouteState.IsSignalControlRaised,
                    IsRouteSecured = route.RouteState.IsRouteSecured,
                    IsCtcControlled = route.RouteState.IsCtcControlled
                }
        };
    }

    public async Task<List<RouteData>> GetRoutesWithStateByStationIds(List<string> stationIds)
    {
        var routes = await routeRepository.GetByStationIdsWithState(stationIds);
        return routes.Select(ToRouteData).ToList();
    }

    public async Task<List<RouteData>> GetActiveRoutes()
    {
        var routeIds = await routeRepository.GetIdsWhereRouteSecured();
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