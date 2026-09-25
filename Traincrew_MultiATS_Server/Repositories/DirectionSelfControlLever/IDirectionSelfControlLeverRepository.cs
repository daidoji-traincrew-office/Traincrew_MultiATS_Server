namespace Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;

public interface IDirectionSelfControlLeverRepository
{
    /// <summary>
    /// 開放てこを名前から取得する。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<Models.DirectionSelfControlLever?> GetDirectionSelfControlLeverByNameWithState(string name);

    /// <summary>
    /// 全ての開放てこのIDを取得する。
    /// </summary>
    /// <returns>開放てこのIDのリスト。</returns>
    Task<List<ulong>> GetAllIds();

    /// <summary>
    /// すべての DirectionSelfControlLever を取得する。
    /// </summary>
    /// <returns>DirectionSelfControlLever のリスト。</returns>
    Task<List<Models.DirectionSelfControlLever>> GetAllWithState();

    /// <summary>
    /// DirectionSelfControlLever名からDirectionSelfControlLeverエンティティへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>DirectionSelfControlLever名をキー、DirectionSelfControlLeverエンティティを値とする辞書</returns>
    Task<Dictionary<string, Models.DirectionSelfControlLever>> GetByNamesAsDictionaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// IDを指定してDirectionSelfControlLeverStateのみを取得する(DirectionSelfControlLever本体はJoinしない)
    /// </summary>
    /// <param name="ids">DirectionSelfControlLever ID(=DirectionSelfControlLeverState ID)のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>DirectionSelfControlLeverStateのリスト</returns>
    Task<List<Models.DirectionSelfControlLeverState>> GetStateByIds(IEnumerable<ulong> ids, CancellationToken cancellationToken = default);
}
