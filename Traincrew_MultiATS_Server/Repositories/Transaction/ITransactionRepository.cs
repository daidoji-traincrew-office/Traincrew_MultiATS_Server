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
    /// <param name="isolationLevel">分離レベル</param>
    /// <returns>トランザクションスコープ</returns>
    Task<ITransactionScope> BeginTransactionAsync(IsolationLevel isolationLevel);
}

/// <summary>
/// トランザクションスコープの抽象インターフェース
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}