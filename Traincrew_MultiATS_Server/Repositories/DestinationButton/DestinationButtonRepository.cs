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

    public async Task<List<Models.DestinationButton>> GetRaisedButtonsAsync()
    {
        return await context.DestinationButtons
            .Include(b => b.DestinationButtonState)
            .Where(b => b.DestinationButtonState.IsRaised == RaiseDrop.Raise)
            .ToListAsync();
    }
}