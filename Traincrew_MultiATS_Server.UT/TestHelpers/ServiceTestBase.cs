using Microsoft.Extensions.DependencyInjection;

namespace Traincrew_MultiATS_Server.UT.TestHelpers;

/// <summary>
/// Base class for Service unit tests with DI container support.
/// Provides automatic mock setup for all repositories and services.
/// </summary>
public abstract class ServiceTestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    private readonly ServiceProvider _serviceProvider;

    protected ServiceTestBase()
    {
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        _serviceProvider = services.BuildServiceProvider();
        ServiceProvider = _serviceProvider;
    }

    /// <summary>
    /// Override this method in derived classes to configure test-specific services.
    /// Call services.AddAllMocks() to register all Repository/Service mocks,
    /// then use services.ReplaceMock() or services.UseRealService() to customize.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    protected virtual void ConfigureTestServices(ServiceCollection services)
    {
        // Derived classes should override this and call:
        // services.AddAllMocks();
        // services.ReplaceMock(mockInstance);
        // services.UseRealService<IXxxService, XxxService>();
    }

    /// <summary>
    /// Get a service from the DI container.
    /// </summary>
    /// <typeparam name="T">Service type to retrieve</typeparam>
    /// <returns>Service instance</returns>
    protected T GetService<T>() where T : notnull
        => ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
