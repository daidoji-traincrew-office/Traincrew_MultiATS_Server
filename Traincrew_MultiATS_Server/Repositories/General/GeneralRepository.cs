using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.General;

public class GeneralRepository(ApplicationDbContext context)
{
    public async Task Save<T>(T entity)
    {
        context.Update(entity);
        await context.SaveChangesAsync();
        context.Entry(entity).State = EntityState.Detached;
    }

    public async Task SaveAll<T>(IEnumerable<T> entities)
    {
        context.UpdateRange(entities);
        await context.SaveChangesAsync();
        foreach (var entity in entities)
        {
            context.Entry(entity).State = EntityState.Detached;
        }
    }

    public async Task Delete<T>(T entity)
    {
        context.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAll<T>(IEnumerable<T> entities)
    {
        context.RemoveRange(entities);
        await context.SaveChangesAsync();
    }
}