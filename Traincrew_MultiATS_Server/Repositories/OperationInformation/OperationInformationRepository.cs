using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.OperationInformation;

public class OperationInformationRepository(ApplicationDbContext context) : IOperationInformationRepository
{
    public async Task<List<OperationInformationState>> GetByNowOrderByTypeAndId(DateTime now)
    {
        return await context.OperationInformationStates 
            .Where(x => x.StartTime <= now && now < x.EndTime)
            .OrderByDescending(x => x.Type)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<List<OperationInformationState>> GetAllOrderByTypeAndId()
    {
        return await context.OperationInformationStates
            .OrderByDescending(x => x.Type)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<OperationInformationState> Add(OperationInformationState state)
    {
        context.OperationInformationStates.Add(state);
        await context.SaveChangesAsync();
        return state;
    }
    
    public async Task<OperationInformationState> Update(OperationInformationState state)
    {
        context.OperationInformationStates.Update(state);
        await context.SaveChangesAsync();
        return state;
    }

    public async Task DeleteById(long id)
    {
        var entity = await context.OperationInformationStates.FindAsync(id);
        if (entity != null)
        {
            context.OperationInformationStates.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}
