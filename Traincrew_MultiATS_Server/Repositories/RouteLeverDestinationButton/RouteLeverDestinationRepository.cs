using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;

public class RouteLeverDestinationRepository(ApplicationDbContext dbContext): IRouteLeverDestinationRepository
{
    public Task<List<Models.RouteLeverDestinationButton>> GetAll()
    {
        return dbContext.RouteLeverDestinationButtons.ToListAsync();
    }
}