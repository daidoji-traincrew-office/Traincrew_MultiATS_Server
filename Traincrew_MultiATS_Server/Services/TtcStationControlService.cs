using Microsoft.AspNetCore.Routing;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
using Route = Traincrew_MultiATS_Server.Models.Route;

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
        var ttcWindows = await ttcWindowRepository.GetAllTtcWindowWithState();
        var ttcWindowLinks = await ttcWindowLinkRepository.GetAllTtcWindowLink();
        //窓に対応する軌道回路情報を全取得
        var ttcWindowTrackCircuitIds = await ttcWindowRepository.GetWindowTrackCircuits();
        //窓リンク関係の進路情報を全取得
        var ttcWindowLinkRouteConditions = await ttcWindowLinkRepository.GetAllTtcWindowLinkRouteConditions();

        //軌道回路情報から軌道回路IDを取得
        var trackCircuitIds = ttcWindowTrackCircuitIds.Select(obj => obj.TrackCircuitId).ToList();
        //窓リンク条件から軌道回路IDを取得
        var ttcWindowLinkTrackCircuitIds = ttcWindowLinks.Select(obj => obj.TrackCircuitCondition).ToList();
        //trackCircuitIdsとttcWindowLinkTrackCircuitIdsを結合して重複を排除したリストを作成
        trackCircuitIds.AddRange(ttcWindowLinkTrackCircuitIds.Where(obj => obj != null).Select(obj => obj.Value));

        var routeIds = ttcWindowLinkRouteConditions.Select(obj => obj.RouteId).ToList();

        //窓と窓に対応する軌道回路IDを関連付けるために、窓名をキーとする辞書を作成
        var ttcWindowTrackCircuitIdsDic = ttcWindowTrackCircuitIds
            .GroupBy(obj => obj.TtcWindowName)
            .ToDictionary(group => group.Key, group => group.Select(obj => obj.TrackCircuitId).ToList());

        //軌道回路IDに対応するオブジェクトを取得し、IDをキーとする辞書を作成
        //trackCircuitIdsとttcWindowLinkTrackCircuitIdsどちらかに含まれる軌道回路オブジェクトを取得
        var trackCircuits = (await trackCircuitRepository.GetTrackCircuitsById(trackCircuitIds))
            .ToDictionary(obj => obj.Id);

        //進路IDに対応する進路オブジェクトを取得
        var routes = (await routeRepository.GetByIdsWithState(routeIds))
            .ToDictionary(obj => obj.Id);

        foreach (var ttcWindow in ttcWindows)
        {
            //窓と窓に対応する軌道回路ID辞書から軌道回路IDを取得
            if (ttcWindowTrackCircuitIdsDic.TryGetValue(ttcWindow.Name, out var trackCircuitIdsList))
            {
                //軌道回路IDに対応するオブジェクトを取得
                var trackCircuitsList = trackCircuitIdsList
                    .Select(id => trackCircuits.GetValueOrDefault(id))
                    .Where(obj => obj != null)
                    .ToList();
                //軌道回路IDに対応するオブジェクトが存在する場合、列番を取得
                if (trackCircuitsList.Count > 0)
                {
                    var trainNumber = trackCircuitsList.First().TrackCircuitState.TrainNumber;
                    ttcWindow.TtcWindowState.TrainNumber = trainNumber;
                    await generalRepository.Save(ttcWindow.TtcWindowState);
                }
            }
            //その窓からの移行処理を考える
            await TrainTrackingProcess(
                ttcWindow.Name,
                ttcWindowLinks,
                ttcWindows,
                ttcWindowLinkRouteConditions,
                trackCircuits,
                routes
            );
        }
    }
    private async Task TrainTrackingProcess(
        string SourceTtcWindowName,
        List<TtcWindowLink> ttcWindowLinks,
        List<TtcWindow> ttcWindows,
        List<TtcWindowLinkRouteCondition> ttcWindowLinkRouteConditions,
        Dictionary<ulong, TrackCircuit> trackCircuits,
        Dictionary<ulong, Route> routes
        )
    {
        //対象窓名に対応する窓リンクを取得
        var ttcWindowLink = ttcWindowLinks.FirstOrDefault(obj => obj.SourceTtcWindowName == SourceTtcWindowName);
        //窓リンクに対応する前窓と後窓を取得
        var sourceTtcWindow = ttcWindows.FirstOrDefault(obj => obj.Name == ttcWindowLink.SourceTtcWindowName);
        var targetTtcWindow = ttcWindows.FirstOrDefault(obj => obj.Name == ttcWindowLink.TargetTtcWindowName);
        //ウィンドウがない場合はスキップ
        if (sourceTtcWindow == null || targetTtcWindow == null)
        {
            return;
        }
        //前窓の列番が空の場合はスキップ
        if (sourceTtcWindow.TtcWindowState.TrainNumber == string.Empty)
        {
            return;
        }
        //空送り対応リンクの場合は、後窓に埋まってなければ移動
        if (ttcWindowLink.IsEmptySending)
        {
            if (targetTtcWindow.TtcWindowState.TrainNumber == string.Empty)
            {
                targetTtcWindow.TtcWindowState.TrainNumber = sourceTtcWindow.TtcWindowState.TrainNumber;
                sourceTtcWindow.TtcWindowState.TrainNumber = string.Empty;
            }
            await generalRepository.Save(sourceTtcWindow.TtcWindowState);
            await generalRepository.Save(targetTtcWindow.TtcWindowState);
            //再起呼出してその次に行かないか確認する
            await TrainTrackingProcess(targetTtcWindow.Name, ttcWindowLinks, ttcWindows, ttcWindowLinkRouteConditions, trackCircuits, routes);
        }

        //
        if (ttcWindowLink.TrackCircuitCondition == null)
        {
            //空送り不能な軌道回路条件なしリンクisなに
            return;
        }

        //窓リンクに対応する軌道回路オブジェクトを取得
        var trackCircuit = trackCircuits.GetValueOrDefault(ttcWindowLink.TrackCircuitCondition.Value);


        if (trackCircuit.TrackCircuitState.IsShortCircuit)
        {
            var TtcWindowLinkRouteConditions = ttcWindowLinkRouteConditions.FirstOrDefault(obj => obj.TtcWindowLinkId == ttcWindowLink.Id);
            var routeState = routes.GetValueOrDefault(TtcWindowLinkRouteConditions.RouteId)?.RouteState;
            if (routeState.IsLeverRelayRaised == RaiseDrop.Raise && routeState.IsApproachLockMRRaised == RaiseDrop.Raise)
            {
                //本当なら増結解結関連で処理をしないといけない
                //現在はあとから入ってきたほうで強制上書きする                 
                targetTtcWindow.TtcWindowState.TrainNumber = sourceTtcWindow.TtcWindowState.TrainNumber;
                sourceTtcWindow.TtcWindowState.TrainNumber = string.Empty;
                await generalRepository.Save(sourceTtcWindow.TtcWindowState);
                await generalRepository.Save(targetTtcWindow.TtcWindowState);
                //再起呼出してその次に行かないか確認する
                await TrainTrackingProcess(targetTtcWindow.Name, ttcWindowLinks, ttcWindows, ttcWindowLinkRouteConditions, trackCircuits, routes);
            }
        }
    }

    public async Task<List<TtcWindow>> GetTtcWindowsByStationIdsWithState(List<string> stationIds)
    {
        return await ttcWindowRepository.GetTtcWindowsByStationIdsWithState(stationIds);
    }
}
