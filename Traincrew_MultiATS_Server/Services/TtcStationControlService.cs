using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;

namespace Traincrew_MultiATS_Server.Services;

public interface ITtcStationControlService
{
    Task<List<TtcWindow>> GetTtcWindowsByStationIdsWithState(List<string> stationIds);
    Task ClearTtcWindowByTrainNumber(string trainNumber);
}

public class TtcStationControlService(
    ITtcWindowRepository ttcWindowRepository,
    IGeneralRepository generalRepository
) : ITtcStationControlService
{
    public async Task<List<TtcWindow>> GetTtcWindowsByStationIdsWithState(List<string> stationIds)
    {
        return await ttcWindowRepository.GetTtcWindowsByStationIdsWithState(stationIds);
    }

    public async Task ClearTtcWindowByTrainNumber(string DiaName)
    {
        //指定された列番が入っている窓を全て取得
        var ttcWindows = await ttcWindowRepository.GetTtcWindowsByTrainNumber(DiaName);
        //列番が入っている窓がない場合はスキップ
        if (ttcWindows.Count == 0)
        {
            return;
        }

        //窓から列番を削除
        foreach (var ttcWindow in ttcWindows)
        {
            ttcWindow.TtcWindowState.TrainNumber = string.Empty;
            await generalRepository.Save(ttcWindow.TtcWindowState);
        }
    }
}
