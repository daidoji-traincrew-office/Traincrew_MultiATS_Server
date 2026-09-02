namespace Traincrew_MultiATS_Server.Repositories.General;

public interface IGeneralRepository
{
    Task Add<T>(T entity);
    Task Add<T>(T entity, CancellationToken cancellationToken);
    Task AddAll<T>(IEnumerable<T> entities);
    Task AddAll<T>(IEnumerable<T> entities, CancellationToken cancellationToken);
    Task Save<T>(T entity);
    Task Save<T>(T entity, CancellationToken cancellationToken);
    Task SaveAll<T>(IEnumerable<T> entities);
    Task SaveAll<T>(IEnumerable<T> entities, CancellationToken cancellationToken);
    Task Delete<T>(T entity);
    Task Delete<T>(T entity, CancellationToken cancellationToken);
    Task DeleteAll<T>(IEnumerable<T> entities);
    Task DeleteAll<T>(IEnumerable<T> entities, CancellationToken cancellationToken);
}