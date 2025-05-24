namespace Traincrew_MultiATS_Server.Repositories.Mutex;

public interface IMutexRepository
{
    Task<IAsyncDisposable> AcquireAsync(string key);
}
