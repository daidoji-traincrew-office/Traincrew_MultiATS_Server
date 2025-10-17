namespace Traincrew_MultiATS_Server.Repositories.Datetime;

public interface IDateTimeRepository
{
    DateTime GetNow();
    void SetTimeOffset(TimeSpan offset);
    TimeSpan GetTimeOffset();
}