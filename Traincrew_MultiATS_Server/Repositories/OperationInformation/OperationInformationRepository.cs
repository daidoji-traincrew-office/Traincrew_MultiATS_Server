using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.OperationInformation;

public class OperationInformationRepository(ApplicationDbContext context) : IOperationInformationRepository
{
    public async Task<List<OperationInformationState>> GetByNow(DateTime now)
    {
        return await context.OperationInformationStates 
            .Where(x => x.StartTime <= now && now < x.EndTime)
            .ToListAsync();
    }

    public async Task<List<OperationInformationState>> GetAll()
    {
        return await context.OperationInformationStates.ToListAsync();
    }

    public async Task Add(OperationInformationState state)
    {
        context.OperationInformationStates.Add(state);
        await context.SaveChangesAsync();
    }
    
    public async Task Update(OperationInformationState state)
    {
        context.OperationInformationStates.Update(state);
        await context.SaveChangesAsync();
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
