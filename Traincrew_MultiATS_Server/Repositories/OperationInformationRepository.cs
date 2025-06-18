using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories;

public class OperationInformationRepository(ApplicationDbContext context) : IOperationInformationRepository
{
    public async Task<List<OperationInformationState>> GetOperationInformationsByNow(DateTime now)
    {
        return await context.OperationInformationStates 
            .Where(x => x.StartTime <= now && now < x.EndTime)
            .ToListAsync();
    }

    public async Task AddOperationInformation(OperationInformationState state)
    {
        context.OperationInformationStates.Add(state);
        await context.SaveChangesAsync();
    }
    
    public async Task UpdateOperationInformation(OperationInformationState state)
    {
        context.OperationInformationStates.Update(state);
        await context.SaveChangesAsync();
    }
}
