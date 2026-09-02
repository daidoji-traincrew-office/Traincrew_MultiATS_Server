namespace Traincrew_MultiATS_Server.Repositories.TtcWindowLink
{
    public interface ITtcWindowLinkRepository
    {
        Task<List<Models.TtcWindowLink>> GetAllTtcWindowLink();
        Task<List<Models.TtcWindowLink>> GetTtcWindowLinkById(List<ulong> ttcWindowLinkIds);
        Task<List<Models.TtcWindowLinkRouteCondition>> GetAllTtcWindowLinkRouteConditions();
        Task<List<Models.TtcWindowLinkRouteCondition>> ttcWindowLinkRouteConditionsById(ulong ttcWindowLinkId);

        /// <summary>
        /// すべてのTtcWindowLinkの(ソース名, ターゲット名)のペアを取得する
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>(ソース名, ターゲット名)のハッシュセット</returns>
        Task<HashSet<(string Source, string Target)>> GetAllLinkPairsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 変更を保存する
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
