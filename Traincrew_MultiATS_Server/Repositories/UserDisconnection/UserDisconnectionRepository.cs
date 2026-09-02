using System.Data;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Repositories.Transaction;

namespace Traincrew_MultiATS_Server.Repositories.UserDisconnection;

public class UserDisconnectionRepository(
    ApplicationDbContext context,
    ITransactionRepository transactionRepository) : IUserDisconnectionRepository
{
    public async Task<List<ulong>> GetBannedUserIdsAsync()
    {
        return await context.UserDisconnectionStates
            .Select(u => u.UserId)
            .ToListAsync();
    }

    public async Task<bool> IsUserBannedAsync(ulong userId)
    {
        return await context.UserDisconnectionStates
            .AnyAsync(u => u.UserId == userId);
    }

    public async Task BanUserAsync(ulong userId)
    {
        await using var transaction = await transactionRepository.BeginTransactionAsync(IsolationLevel.RepeatableRead);

        var exists = await context.UserDisconnectionStates
            .AnyAsync(u => u.UserId == userId);

        if (!exists)
        {
            await context.UserDisconnectionStates.AddAsync(new()
            {
                UserId = userId
            });
            await context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task UnbanUserAsync(ulong userId)
    {
        await context.UserDisconnectionStates
            .Where(u => u.UserId == userId)
            .ExecuteDeleteAsync();
    }
}