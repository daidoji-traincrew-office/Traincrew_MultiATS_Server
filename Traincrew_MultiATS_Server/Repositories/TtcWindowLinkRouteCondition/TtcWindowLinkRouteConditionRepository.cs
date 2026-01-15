using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TtcWindowLinkRouteCondition;

public class TtcWindowLinkRouteConditionRepository(ApplicationDbContext context) : ITtcWindowLinkRouteConditionRepository
{
    public async Task AddAsync(Models.TtcWindowLinkRouteCondition routeCondition, CancellationToken cancellationToken = default)
    {
        await context.TtcWindowLinkRouteConditions.AddAsync(routeCondition, cancellationToken);
    }
}
