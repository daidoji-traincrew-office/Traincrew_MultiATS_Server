using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.PhoneSession;

public class PhoneSessionRepository(ApplicationDbContext context) : IPhoneSessionRepository
{
    public async Task<PhoneCallSession> CreateSessionAsync(string callerNumber, string callerConnectionId, string targetNumber, DateTime now)
    {
        var session = new PhoneCallSession
        {
            CallerNumber = callerNumber,
            CallerConnectionId = callerConnectionId,
            TargetNumber = targetNumber,
            Status = PhoneCallStatus.Calling,
            CreatedAt = now
        };

        context.PhoneCallSessions.Add(session);
        await context.SaveChangesAsync();
        return session;
    }

    public async Task<PhoneCallSession?> GetActiveSessionByCallerConnectionIdAsync(string callerConnectionId)
    {
        return await context.PhoneCallSessions
            .Where(s => s.CallerConnectionId == callerConnectionId && s.EndedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<PhoneCallSession?> GetActiveSessionByTargetConnectionIdAsync(string targetConnectionId)
    {
        return await context.PhoneCallSessions
            .Where(s => s.TargetConnectionId == targetConnectionId && s.EndedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<PhoneCallSession?> GetActiveSessionByCallerNumberAndTargetNumberAsync(string callerNumber, string targetNumber)
    {
        return await context.PhoneCallSessions
            .Where(s => s.CallerNumber == callerNumber && s.TargetNumber == targetNumber && s.EndedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<PhoneCallSession?> GetActiveSessionByTargetNumberAsync(string targetNumber)
    {
        return await context.PhoneCallSessions
            .Where(s => s.TargetNumber == targetNumber && s.EndedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateStatusAsync(long sessionId, PhoneCallStatus status)
    {
        await context.PhoneCallSessions
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Status, status));
    }

    public async Task SetAnsweredAsync(long sessionId, string targetConnectionId)
    {
        await context.PhoneCallSessions
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.TargetConnectionId, targetConnectionId)
                .SetProperty(s => s.Status, PhoneCallStatus.Answered));
    }

    public async Task EndSessionAsync(long sessionId)
    {
        await context.PhoneCallSessions
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, PhoneCallStatus.Ended)
                .SetProperty(s => s.EndedAt, DateTime.UtcNow));
    }
}
