using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.DestinationButton;

public class DestinationButtonRepository(ApplicationDbContext context) : IDestinationButtonRepository
{
    public async Task<List<Models.DestinationButton>> GetAllWithState()
    {
        return await context.DestinationButtons
            .Include(b => b.DestinationButtonState)
            .ToListAsync();
    }

    public async Task<Models.DestinationButton?> GetButtonByName(string name)
    {
        return await context.DestinationButtons
            .Include(b => b.DestinationButtonState)
            .FirstOrDefaultAsync(button => button.Name == name);
    }

    public async Task<List<Models.DestinationButton>> GetButtonsByStationIds(List<string> stationIds)
    {
        return await context.DestinationButtons
            .Include(b => b.DestinationButtonState)
            .Where(button => stationIds.Contains(button.StationId))
            .ToListAsync();
    }

    public async Task UpdateRaisedButtonsAsync(DateTime now)
    {
        await context.DestinationButtonStates
            .Where(dbs => dbs.IsRaised == RaiseDrop.Raise &&
                        (now - dbs.OperatedAt).TotalSeconds > Constants.Constants.DestinationButtonAutoDropDelay) 
            .ExecuteUpdateAsync(b => 
                b.SetProperty(dbs => dbs.IsRaised, RaiseDrop.Drop)
                 .SetProperty(dbs => dbs.OperatedAt, now));

    }
}