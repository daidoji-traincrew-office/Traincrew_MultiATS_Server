using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TtcWindowDisplayStation;

public class TtcWindowDisplayStationRepository(ApplicationDbContext context) : ITtcWindowDisplayStationRepository
{
    public async Task AddAsync(Models.TtcWindowDisplayStation displayStation, CancellationToken cancellationToken = default)
    {
        await context.TtcWindowDisplayStations.AddAsync(displayStation, cancellationToken);
    }
}
