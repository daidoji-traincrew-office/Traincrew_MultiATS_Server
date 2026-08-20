using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.Server;

namespace Traincrew_MultiATS_Server.Services;

public interface IDateTimeService
{
    /// <summary>
    /// TST(ゲーム内時刻)の現在時刻を、営業日開始時刻(4:00)を基準とした
    /// [4:00, 28:00) の経過時間で取得する
    /// </summary>
    Task<TimeSpan> GetTstNow();
}

public class DateTimeService(
    IDateTimeRepository dateTimeRepository,
    IServerRepository serverRepository) : IDateTimeService
{
    /// <summary>
    /// 営業日の開始時刻。鉄道の営業日は通常4:00から開始される。
    /// </summary>
    public static readonly TimeSpan ServiceDayStartTime = TimeSpan.FromHours(4);

    public async Task<TimeSpan> GetTstNow()
    {
        // 時差を取得
        var timeOffset = await serverRepository.GetTimeOffset();

        // 現在時刻を取得
        var now = dateTimeRepository.GetNow();

        // 時差を加算し、営業日基準に正規化して返す
        return NormalizeToServiceDay(now.AddHours(timeOffset).TimeOfDay);
    }

    /// <summary>
    /// 時刻を営業日開始時刻(4:00)を基準に [4:00, 28:00) の区間へ正規化する。
    /// 0:00〜4:00 は前営業日の 24:00〜28:00 として扱い、28:00 以上や負の値は 24 時間単位で折り返す。
    /// これにより 25:30 のような 24 時超えダイヤと 24 時間未満の現在時刻を直接比較できる。
    /// </summary>
    /// <param name="time">正規化する時刻</param>
    /// <returns>[4:00, 28:00) の範囲に写像された時刻</returns>
    public static TimeSpan NormalizeToServiceDay(TimeSpan time)
    {
        const double secondsPerDay = 86400.0;
        var startSeconds = ServiceDayStartTime.TotalSeconds;
        var offsetFromStart = time.TotalSeconds - startSeconds;

        // C# の % は負の被除数に対して負を返すため、二重剰余で常に非負へ寄せる
        var normalized = (offsetFromStart % secondsPerDay + secondsPerDay) % secondsPerDay;

        return TimeSpan.FromSeconds(normalized + startSeconds);
    }
}
