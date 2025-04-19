namespace Traincrew_MultiATS_Server.Repositories.Datetime;

public class DateTimeRepository : IDateTimeRepository
{
    private readonly DateTime _now = DateTime.Now;

    public DateTime GetNow()
    {
        return _now;
    }
}