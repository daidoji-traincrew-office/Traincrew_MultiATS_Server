
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
        /// <returns>TtcWindow名のハッシュセット</returns>
        Task<HashSet<string>> GetAllWindowNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 変更を保存する
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
