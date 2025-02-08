using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Button;

public class ButtonRepository(ApplicationDbContext context) : IButtonRepository
{
    public async Task<Dictionary<string, Models.Button>> GetAllButtons()
    {
        return await context.Buttons
            .Include(b => b.ButtonState)
            .ToDictionaryAsync(button => button.Name);
    }
}