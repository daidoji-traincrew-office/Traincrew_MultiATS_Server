namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Base class for database initializers
/// </summary>
public abstract class BaseDbInitializer
{

    /// <summary>
    ///     Initialize database entities
    /// </summary>
    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);
}