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
    /// 読み取り専用のトランザクションを開始します(<c>SET TRANSACTION READ ONLY</c>)。
    /// 分離レベルを省略したときは DB の既定です。読み終えたら <see cref="ITransactionScope.CommitAsync()"/> を呼びます。
    /// </summary>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginReadOnlyTransactionAsync();

    /// <summary>
    /// 読み取り専用のトランザクションを開始します。
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginReadOnlyTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 読み取り専用のトランザクションを開始します。
    /// </summary>
    /// <param name="isolationLevel">分離レベル</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginReadOnlyTransactionAsync(IsolationLevel isolationLevel);

    /// <summary>
    /// 読み取り専用のトランザクションを開始します。
    /// </summary>
    /// <param name="isolationLevel">分離レベル</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginReadOnlyTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken);
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