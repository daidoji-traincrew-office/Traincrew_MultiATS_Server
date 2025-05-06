using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;

namespace Traincrew_MultiATS_Server.Services;

public class TtcStationControlService(
    ITtcWindowRepository ttcWindowRepository,
    ITtcWindowLinkRepository ttcWindowLinkRepository,
    IInterlockingObjectRepository interlockingObjectRepository,
    ITrackCircuitRepository trackCircuitRepository,
    IRouteRepository routeRepository,
    IGeneralRepository generalRepository
    )
{
    public async Task TrainTracking()
    {
        //窓・窓リンクを全取得
        var ttcWindows = await ttcWindowRepository.GetAllTtcWindow();
        var ttcWindowLinks = await ttcWindowLinkRepository.GetAllTtcWindowLink();
        //窓に対応する軌道回路情報を全取得
        var ttcWindowTrackCircuitIds = await ttcWindowRepository.GetWindowTrackCircuits();
        //窓リンク関係の進路情報を全取得
        var ttcWindowLinkRouteConditions = await ttcWindowLinkRepository.GetAllTtcWindowLinkRouteConditions();

        //軌道回路情報から軌道回路IDを取得
        var trackCircuitIds = ttcWindowTrackCircuitIds.Select(obj => obj.TrackCircuitId).ToList();
        //進路情報から進路IDを取得
        var routeIds = ttcWindowLinkRouteConditions.Select(obj => obj.RouteId).ToList();

        //軌道回路IDと進路IDに対応するオブジェクトを取得
        var trackCircuits = await trackCircuitRepository.GetTrackCircuitsById(trackCircuitIds);
        var routes = await routeRepository.GetByIdsWithState(routeIds);

        foreach (var ttcWindowLink in ttcWindowLinks)
        {
            //窓リンクに対応する前窓と後窓を取得
            var frontTtcWindow = ttcWindows.FirstOrDefault(obj => obj.Name == ttcWindowLink.SourceTtcWindowName);
            var backTtcWindow = ttcWindows.FirstOrDefault(obj => obj.Name == ttcWindowLink.TargetTtcWindowName);

        }
    }
}
