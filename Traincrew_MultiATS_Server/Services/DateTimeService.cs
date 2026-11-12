using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.Server;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 時刻(現在時刻・JST・TST・運用日・連動ハートビートの鮮度)を扱う。
/// 現在時刻に依存するものはインスタンスメソッド(Mock で差し替える)、時刻を受け取って計算するだけのものは static。
/// </summary>
public interface IDateTimeService
{
    /// <summary>現在のTST(サーバーのローカル時刻 + server_state の時差)を返す。</summary>
    Task<DateTime> GetTstNow();

    /// <summary>現在のUTCを返す。呼ぶたびにその時点の時刻を返す。</summary>
    DateTimeOffset GetUtcNow();

    /// <summary>現在のJSTを、タイムゾーンなしの日時で返す(ホストのタイムゾーンに依存しない)。</summary>
    DateTime GetJstNow();

    /// <summary>
    /// 現在のJSTに対する連動ハートビートの経過が2秒未満なら true(未記録なら false)。
    /// 時刻の判定だけを行う。連動が健全か(全信号リレーの扛上など)は呼び出し側で合わせて判定する。
    /// </summary>
    bool IsInterlockingHeartbeatFresh(DateTime? heartbeatJst);
}

public class DateTimeService(
    IDateTimeRepository dateTimeRepository,
    IServerRepository serverRepository) : IDateTimeService
{
    /// <summary>日本標準時のUTCオフセット。</summary>
    public static readonly TimeSpan JapanOffset = TimeSpan.FromHours(9);

    /// <summary>運用日(システム日付)が切り替わる時刻。これより前は前日の運用日として扱う。</summary>
    public static readonly TimeSpan ServiceDayStartTime = TimeSpan.FromHours(4);

    /// <summary>連動ハートビートを新鮮とみなす経過時間の上限(この値を含まない)。</summary>
    public static readonly TimeSpan InterlockingHeartbeatMaximumAge = TimeSpan.FromSeconds(2);

    public async Task<DateTime> GetTstNow()
    {
        // 時差を取得
        var timeOffset = await serverRepository.GetTimeOffset();

        // 現在時刻を取得
        var now = dateTimeRepository.GetNow();

        // 時差を加算してTSTを返す
        return ToTst(now, timeOffset);
    }

    public DateTimeOffset GetUtcNow()
    {
        return dateTimeRepository.GetUtcNow();
    }

    public DateTime GetJstNow()
    {
        return GetUtcNow().ToOffset(JapanOffset).DateTime;
    }

    public bool IsInterlockingHeartbeatFresh(DateTime? heartbeatJst)
    {
        return heartbeatJst is { } heartbeat && GetJstNow() - heartbeat < InterlockingHeartbeatMaximumAge;
    }

    /// <summary>JST(またはサーバーのローカル時刻)に時差を加えてTSTにする。</summary>
    public static DateTime ToTst(DateTime jst, int timeOffsetHours)
    {
        return jst.AddHours(timeOffsetHours);
    }

    /// <summary>TSTから、04:00を境界にした運用日(システム日付)を返す。</summary>
    public static DateOnly GetServiceDate(DateTime tst)
    {
        return DateOnly.FromDateTime(tst.TimeOfDay < ServiceDayStartTime ? tst.AddDays(-1) : tst);
    }
}
