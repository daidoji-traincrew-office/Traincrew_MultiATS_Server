
namespace Traincrew_MultiATS_Server.Repositories.InterlockingObject;

public interface IInterlockingObjectRepository
{

    public Task<List<Models.InterlockingObject>> GetAllWithState();
    /**
     * 軌道回路、進路、転轍機を取得する(状態なし)
     * @param ids IDリスト
     * @return 軌道回路、進路、転轍機リスト
     */
    public Task<List<Models.InterlockingObject>> GetObjectByIds(IEnumerable<ulong> ids);
    /// <summary>
    /// 軌道回路、進路、転轍機を取得する(状態付き)
    /// </summary>
    /// <param name="ids">IDリスト</param>
    /// <returns>軌道回路、進路、転轍機リスト</returns>
    public Task<List<Models.InterlockingObject>> GetObjectByIdsWithState(IEnumerable<ulong> ids);
    public Task<Models.InterlockingObject> GetObject(string name);
    public Task<List<Models.InterlockingObject>> GetObjectsByStationIdsWithState(List<string> stationIds);

    /// <summary>
    /// すべての連動オブジェクトを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>連動オブジェクトのリスト</returns>
    public Task<List<Models.InterlockingObject>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 連動オブジェクトを更新する
    /// </summary>
    /// <param name="interlockingObject">更新する連動オブジェクト</param>
    public void Update(Models.InterlockingObject interlockingObject);

    /// <summary>
    /// 変更を保存する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}