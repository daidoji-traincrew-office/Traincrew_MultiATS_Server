namespace Traincrew_MultiATS_Server.Repositories.Datetime;

public class DateTimeRepository : IDateTimeRepository
{
    private readonly DateTime _now = DateTime.Now;
    private TimeSpan _timeOffset = TimeSpan.Zero;

    public DateTime GetNow()
    {
        return _now.Add(_timeOffset);
    }

    public void SetTimeOffset(TimeSpan offset)
    {
        _timeOffset = offset;
    }

    public TimeSpan GetTimeOffset()
    {
        return _timeOffset;
    }
}