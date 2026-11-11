namespace Traincrew_MultiATS_Server.Repositories.Datetime;

/// <summary>連動ハートビートの記録と健全性判定に使うJST時計。</summary>
public static class InterlockingHeartbeatClock
{
    private static readonly TimeSpan JapanOffset = TimeSpan.FromHours(9);
    private static readonly TimeSpan MaximumAge = TimeSpan.FromSeconds(2);

    /// <summary>タイムゾーンなしのDB列に記録する現在のJSTを返す。</summary>
    public static DateTime GetCurrentJst() => DateTimeOffset.UtcNow.ToOffset(JapanOffset).DateTime;

    /// <summary>全信号リレーが扛上し、JSTのハートビートの経過が2秒未満なら健全とする。</summary>
    public static bool IsHealthy(bool isAllSignalRelayRaised, DateTime? heartbeatJst, DateTime nowJst)
    {
        if (!isAllSignalRelayRaised || heartbeatJst is not { } heartbeat)
        {
            return false;
        }

        var age = nowJst - heartbeat;
        return age < MaximumAge;
    }
}
