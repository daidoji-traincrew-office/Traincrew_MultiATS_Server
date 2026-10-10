
namespace Traincrew_MultiATS_Server.Repositories.TtcWindow
{
    public interface ITtcWindowRepository
    {
        Task<List<Models.TtcWindow>> GetAllTtcWindowWithState();
        Task<List<Models.TtcWindow>> GetTtcWindowWithStateByName(List<string> name);
        Task<List<Models.TtcWindowTrackCircuit>> GetWindowTrackCircuits();
        Task<List<Models.TtcWindowTrackCircuit>> ttcWindowTrackCircuitsById(List<string> ttcWindowName);
        Task<List<Models.TtcWindow>> GetTtcWindowsByStationIdsWithState(List<string> stationIds);
        Task<List<Models.TtcWindow>> GetTtcWindowsByTrainNumber(string diaName);

        /// <summary>
        /// すべてのTtcWindow名を取得する
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>TtcWindow名のList</returns>
        Task<List<string>> GetAllWindowNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 変更を保存する
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 全件のTtcWindowStateを取得する。<b>配信(読み取り専用)専用</b>。
        /// </summary>
        /// <remarks>
        /// AsNoTrackingで取得する(追跡しない)。取得した状態を更新・Saveに使ってはならない。
        /// 更新用の取得には既存のGetStateByIds等を使うこと。
        /// </remarks>
        Task<List<Models.TtcWindowState>> GetAllStatesForBroadcast(CancellationToken cancellationToken = default);
    }
}
