namespace Traincrew_MultiATS_Server.Repositories.Datetime;

public class DateTimeRepository : IDateTimeRepository
{
    private readonly DateTime _now = DateTime.Now;

    public DateTime GetNowScoped()
    {
        return _now;
    }

    public DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
