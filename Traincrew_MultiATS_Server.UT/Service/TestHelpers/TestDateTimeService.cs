using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service.TestHelpers;

/// <summary>
/// DateTimeServiceのテスト用ヘルパークラス
/// JST現在時刻と時差からTST現在時刻を返す
/// </summary>
public class TestDateTimeService : IDateTimeService
{
    private DateTime jstNow = DateTime.MinValue;
    private int timeOffset;

    /// <summary>
    /// JST現在時刻と時差を設定
    /// </summary>
    public void SetupNow(DateTime jstNow, int timeOffset = 0)
    {
        this.jstNow = jstNow;
        this.timeOffset = timeOffset;
    }

    public Task<DateTime> GetTstNow()
    {
        return Task.FromResult(jstNow.AddHours(timeOffset));
    }
}
