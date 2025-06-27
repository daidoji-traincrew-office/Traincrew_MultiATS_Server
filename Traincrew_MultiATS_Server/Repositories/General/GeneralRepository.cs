using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.General;

public class GeneralRepository(ApplicationDbContext context) : IGeneralRepository
{
    public async Task Add<T>(T entity)
    {
        context.Add(entity);
        await context.SaveChangesAsync();
        context.Entry(entity).State = EntityState.Detached;
    }

    public async Task AddAll<T>(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            context.Add(entity);
        }
        await context.SaveChangesAsync();
        foreach (var entity in entities)
        {
            context.Entry(entity).State = EntityState.Detached;
        }
    }

    public async Task Save<T>(T entity)
    {
        context.Update(entity);
        await context.SaveChangesAsync();
        context.Entry(entity).State = EntityState.Detached;
    }

    public async Task SaveAll<T>(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            context.Update(entity);
        }
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
        foreach (var entity in entities)
        {
            context.Remove(entity); 
        }
        await context.SaveChangesAsync();
    }
}