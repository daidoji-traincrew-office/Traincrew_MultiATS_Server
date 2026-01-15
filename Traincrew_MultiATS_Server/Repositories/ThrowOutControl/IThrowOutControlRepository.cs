using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

public interface IThrowOutControlRepository
{
    Task<List<Models.ThrowOutControl>> GetAll();
    Task<List<Models.ThrowOutControl>> GetBySourceIds(List<ulong> sourceIds);
    Task<List<Models.ThrowOutControl>> GetBySourceIdsAndTypes(List<ulong> sourceIds, List<ThrowOutControlType> controlTypes);
    Task<List<Models.ThrowOutControl>> GetByTargetIds(List<ulong> targetIds);
    Task<List<Models.ThrowOutControl>> GetByTargetIdsAndTypes(List<ulong> targetIds, List<ThrowOutControlType> controlTypes);
    Task<List<Models.ThrowOutControl>> GetBySourceAndTargetIds(List<ulong> ids);
    Task<List<Models.ThrowOutControl>> GetByControlTypes(List<ThrowOutControlType> controlTypes);

    /// <summary>
    /// すべてのThrowOutControlのSourceIdとTargetIdのペアを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>SourceIdとTargetIdのペアのHashSet</returns>
    Task<HashSet<(ulong SourceId, ulong TargetId)>> GetAllPairsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ThrowOutControlを追加する
    /// </summary>
    /// <param name="throwOutControl">追加するThrowOutControl</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task AddAsync(Models.ThrowOutControl throwOutControl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 変更を保存する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}