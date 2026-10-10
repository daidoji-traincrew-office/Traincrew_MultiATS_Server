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

    /// <summary>
    /// 読み取り専用のトランザクションを開始します。
    /// </summary>
    /// <remarks>
    /// 開始直後(最初のクエリより前)に <c>SET TRANSACTION READ ONLY</c> を実行する。
    /// トランザクション内の書き込みはDBが拒否する(SQLSTATE 25006 read_only_sql_transaction)。
    /// 読み取りだけのスナップショット(配信)専用。
    /// </remarks>
    /// <param name="isolationLevel">分離レベル</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginReadOnlyTransactionAsync(
        IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
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