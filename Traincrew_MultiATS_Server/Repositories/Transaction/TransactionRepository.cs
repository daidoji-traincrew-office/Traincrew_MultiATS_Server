using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Transaction;

/// <summary>
/// EF Core用のトランザクションリポジトリ実装
/// </summary>
public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
{
    public async Task<ITransactionScope> BeginTransactionAsync()
    {
        var transaction = await context.Database.BeginTransactionAsync();
        return new EfCoreTransactionScope(transaction);
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfCoreTransactionScope(transaction);
    }

    public async Task<ITransactionScope> BeginTransactionAsync(IsolationLevel isolationLevel)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
        return new EfCoreTransactionScope(transaction);
    }

    public async Task<ITransactionScope> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new EfCoreTransactionScope(transaction);
    }

    public async Task<ITransactionScope> BeginReadOnlyTransactionAsync()
    {
        var transaction = await context.Database.BeginTransactionAsync();
        return await SetReadOnlyAsync(transaction, CancellationToken.None);
    }

    public async Task<ITransactionScope> BeginReadOnlyTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return await SetReadOnlyAsync(transaction, cancellationToken);
    }

    public async Task<ITransactionScope> BeginReadOnlyTransactionAsync(IsolationLevel isolationLevel)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
        return await SetReadOnlyAsync(transaction, CancellationToken.None);
    }

    public async Task<ITransactionScope> BeginReadOnlyTransactionAsync(IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return await SetReadOnlyAsync(transaction, cancellationToken);
    }

    // SET TRANSACTION READ ONLY は、トランザクションの最初の文として実行する必要がある。
    private async Task<ITransactionScope> SetReadOnlyAsync(IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", cancellationToken);
            return new EfCoreTransactionScope(transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}

public class EfCoreTransactionScope(IDbContextTransaction transaction) : ITransactionScope
{

    public async Task CommitAsync()
    {
        await transaction.CommitAsync();
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync()
    {
        await transaction.RollbackAsync();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
    }
}