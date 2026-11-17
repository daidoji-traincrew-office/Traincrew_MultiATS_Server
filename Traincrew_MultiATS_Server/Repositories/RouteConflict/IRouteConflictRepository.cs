namespace Traincrew_MultiATS_Server.Repositories.RouteConflict;

public interface IRouteConflictRepository
{
    /// <summary>
    /// 登録済みの競合進路をすべて取得する
    /// </summary>
    Task<List<Models.RouteConflict>> GetAll(CancellationToken cancellationToken = default);
}
