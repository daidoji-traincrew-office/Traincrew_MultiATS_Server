using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.PhoneSession;

public interface IPhoneSessionRepository
{
    Task<PhoneCallSession> CreateSessionAsync(string callerNumber, string callerConnectionId, string targetNumber);
    Task<PhoneCallSession?> GetActiveSessionByCallerConnectionIdAsync(string callerConnectionId);
    Task<PhoneCallSession?> GetActiveSessionByTargetConnectionIdAsync(string targetConnectionId);
    Task UpdateStatusAsync(long sessionId, PhoneCallStatus status);
    Task SetAnsweredAsync(long sessionId, string targetConnectionId);
    Task EndSessionAsync(long sessionId);
}
