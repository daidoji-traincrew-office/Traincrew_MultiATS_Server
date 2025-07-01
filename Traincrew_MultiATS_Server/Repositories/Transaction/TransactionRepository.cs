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

    public async Task<ITransactionScope> BeginTransactionAsync(IsolationLevel isolationLevel)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
        return new EfCoreTransactionScope(transaction);
    }
}

public class EfCoreTransactionScope(IDbContextTransaction transaction) : ITransactionScope
{

    public async Task CommitAsync()
    {
        await transaction.CommitAsync();
    }
    public async Task RollbackAsync()
    {
        await transaction.RollbackAsync();
    }
    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
    }
}