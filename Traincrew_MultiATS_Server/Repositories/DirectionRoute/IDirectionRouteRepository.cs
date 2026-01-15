namespace Traincrew_MultiATS_Server.Repositories.DirectionRoute;

public interface IDirectionRouteRepository
{
    /// <summary>
    /// 全ての方向てこのIDを取得する。
    /// </summary>
    /// <returns>方向てこのIDのリスト。</returns>
    Task<List<ulong>> GetAllIds();

    /// <summary>
    /// すべての DirectionRoute を取得する。
    /// </summary>
    /// <returns>DirectionRoute のリスト。</returns>
    Task<List<Models.DirectionRoute>> GetAllWithState();

    /// <summary>
    /// DirectionRoute名からIDへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>DirectionRoute名をキー、IDを値とする辞書</returns>
    Task<Dictionary<string, ulong>> GetIdsByNameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// DirectionRoute名からDirectionRouteエンティティへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>DirectionRoute名をキー、DirectionRouteエンティティを値とする辞書</returns>
    Task<Dictionary<string, Models.DirectionRoute>> GetByNamesAsDictionaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// DirectionRouteを更新する
    /// </summary>
    /// <param name="directionRoute">更新するDirectionRoute</param>
    void Update(Models.DirectionRoute directionRoute);

    /// <summary>
    /// 変更を保存する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
