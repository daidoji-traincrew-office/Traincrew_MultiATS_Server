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
            if (ttcWindow.Type == TtcWindowType.HomeTrack || (ttcWindow.Type == TtcWindowType.Switching &&
                                                              ttcWindow.TtcWindowState.TrainNumber != null))
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
                        if (trackCircuitsList.Any(t => t.TrackCircuitState.IsShortCircuit))
                        {
                            var trainNumber = trackCircuitsList.First().TrackCircuitState.TrainNumber;
                            //その列番が短絡している軌道回路をtrackCircuitsから全部取得
                            var shortCircuitTrackCircuits = trackCircuits.ToList()
                                .Where(obj => obj.Value.TrackCircuitState.TrainNumber == trainNumber)
                                .Select(obj => obj.Value)
                                .ToList();

                            //trackCircuitsListとshortCircuitTrackCircuitsの軌道回路の数が一致する場合のみ、列番を設定する
                            if (shortCircuitTrackCircuits.Count == trackCircuitsList.Count)
                            {
                                ttcWindow.TtcWindowState.TrainNumber = trainNumber;
                                await generalRepository.Save(ttcWindow.TtcWindowState);
                            }
                        }
                    }
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
        //対象窓名に対応する窓リンクを全て取得
        var targetTtcWindowLinks = ttcWindowLinks
            .Where(obj => obj.SourceTtcWindowName == SourceTtcWindowName)
            .ToList();
        foreach (var ttcWindowLink in targetTtcWindowLinks)
        {
            if (ttcWindowLink == null)
            {
                continue;
            }

            //窓リンクに対応する前窓と後窓を取得
            var sourceTtcWindow = ttcWindows.FirstOrDefault(obj => obj.Name == ttcWindowLink.SourceTtcWindowName);
            var targetTtcWindow = ttcWindows.FirstOrDefault(obj => obj.Name == ttcWindowLink.TargetTtcWindowName);
            //ウィンドウがない場合はスキップ
            if (sourceTtcWindow == null || targetTtcWindow == null)
            {
                continue;
            }

            //前窓の列番が空の場合はスキップ
            if (sourceTtcWindow.TtcWindowState.TrainNumber == string.Empty)
            {
                continue;
            }

            //空送り対応リンクの場合は、後窓に埋まってなければ移動
            if (ttcWindowLink.IsEmptySending)
            {
                //行先の窓に列番が入っていない場合は、移動する
                if (targetTtcWindow.TtcWindowState.TrainNumber == string.Empty)
                {
                    targetTtcWindow.TtcWindowState.TrainNumber = sourceTtcWindow.TtcWindowState.TrainNumber;
                    sourceTtcWindow.TtcWindowState.TrainNumber = string.Empty;
                    await generalRepository.Save(sourceTtcWindow.TtcWindowState);
                    await generalRepository.Save(targetTtcWindow.TtcWindowState);
                    //再起呼出してその次に行かないか確認する
                    await TrainTrackingProcess(targetTtcWindow.Name, ttcWindowLinks, ttcWindows,
                        ttcWindowLinkRouteConditions, trackCircuits, routes);
                }
                //行先の窓と前窓の列番が同じ場合は、前窓から削除する
                else if (targetTtcWindow.TtcWindowState.TrainNumber == sourceTtcWindow.TtcWindowState.TrainNumber)
                {
                    sourceTtcWindow.TtcWindowState.TrainNumber = string.Empty;
                    await generalRepository.Save(sourceTtcWindow.TtcWindowState);
                }

                continue;
            }

            if (ttcWindowLink.TrackCircuitCondition == null)
            {
                //空送り不能な軌道回路条件なしリンクisなに             
                continue;
            }

            //窓リンクに対応する軌道回路オブジェクトを取得
            var trackCircuit = trackCircuits.GetValueOrDefault(ttcWindowLink.TrackCircuitCondition.Value);


            if (!trackCircuit.TrackCircuitState.IsShortCircuit)
            {
                continue;
            }

            var trainNumber = trackCircuit.TrackCircuitState.TrainNumber;
            var TtcWindowLinkRouteConditions =
                ttcWindowLinkRouteConditions.FirstOrDefault(obj => obj.TtcWindowLinkId == ttcWindowLink.Id);
            var routeState = routes.GetValueOrDefault(TtcWindowLinkRouteConditions.RouteId)?.RouteState;
            if (!(routeState.IsRouteLockRaised == RaiseDrop.Drop || routeState.IsLeverRelayRaised == RaiseDrop.Raise))
            {
                continue;
            }
            //本当なら増結解結関連で処理をしないといけない
            //現在はあとから入ってきたほうで強制上書きする              
            if (targetTtcWindow.TtcWindowState.TrainNumber != trainNumber)
            {
                targetTtcWindow.TtcWindowState.TrainNumber = sourceTtcWindow.TtcWindowState.TrainNumber;
            }

            sourceTtcWindow.TtcWindowState.TrainNumber = string.Empty;
            await generalRepository.Save(sourceTtcWindow.TtcWindowState);
            await generalRepository.Save(targetTtcWindow.TtcWindowState);
            //再起呼出してその次に行かないか確認する
            await TrainTrackingProcess(targetTtcWindow.Name, ttcWindowLinks, ttcWindows,
                ttcWindowLinkRouteConditions, trackCircuits, routes);

        }
    }

    public async Task<List<TtcWindow>> GetTtcWindowsByStationIdsWithState(List<string> stationIds)
    {
        return await ttcWindowRepository.GetTtcWindowsByStationIdsWithState(stationIds);
    }
}