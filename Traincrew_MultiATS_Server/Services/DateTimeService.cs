using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.Server;

namespace Traincrew_MultiATS_Server.Services;

public class DateTimeService(
    IDateTimeRepository dateTimeRepository,
    IServerRepository serverRepository)
{
    public async Task<DateTime> GetTstNow()
    {
        // 時差を取得
        var timeOffset = await serverRepository.GetTimeOffset();

        // 現在時刻を取得
        var now = dateTimeRepository.GetNow();

        // 時差を加算してTSTを返す
        return now.AddHours(timeOffset);
    }
}
