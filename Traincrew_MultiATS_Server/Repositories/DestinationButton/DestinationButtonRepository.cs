using Microsoft.EntityFrameworkCore;
using Polly;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.DestinationButton;

public class DestinationButtonRepository(ApplicationDbContext context) : IDestinationButtonRepository
{
    public async Task<Dictionary<string, Models.DestinationButton>> GetAllButtons()
    {
        return await context.DestinationButtons
            .Include(b => b.DestinationButtonState)
            .ToDictionaryAsync(button => button.Name);
    }

    public async Task<Models.DestinationButton?> GetButtonByName(string name)
    {
        return await context.Set<Models.DestinationButton>()
            .FirstOrDefaultAsync(button => button.Name == name);
    }
}