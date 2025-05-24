using System.Collections.Concurrent;

namespace Traincrew_MultiATS_Server.Repositories.Mutex;

public class MutexRepository: IMutexRepository
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _mutexes = new();

    public async Task<IAsyncDisposable> AcquireAsync(string key)
    {
        var semaphore = _mutexes.GetOrAdd(key, _ => new(1, 1));
        await semaphore.WaitAsync();
        return new Releaser(() =>
        {
            semaphore.Release();
        });
    }

    private class Releaser(Action release) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            release();
            return ValueTask.CompletedTask;
        }
    }
}
