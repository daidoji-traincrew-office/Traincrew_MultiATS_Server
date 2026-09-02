using System.Data;

namespace Traincrew_MultiATS_Server.Repositories.Transaction;

/// <summary>
/// DBトランザクションの抽象リポジトリ
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// トランザクションを開始します。
    /// </summary>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginTransactionAsync();

    /// <summary>
    /// トランザクションを開始します。
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// トランザクションを開始します。
    /// </summary>
    /// <param name="isolationLevel">分離レベル</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginTransactionAsync(IsolationLevel isolationLevel);

    /// <summary>
    /// トランザクションを開始します。
    /// </summary>
    /// <param name="isolationLevel">分離レベル</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken);
}

/// <summary>
/// トランザクションスコープの抽象インターフェース
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync();
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync();
    Task RollbackAsync(CancellationToken cancellationToken);
}