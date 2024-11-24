using Microsoft.EntityFrameworkCore;

namespace Traincrew_MultiATS_Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Models.Station> Stations { get; set; }
    public DbSet<Models.Circuit> Circuits { get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Convert all column names to snake_case 
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }
    
    private static string ToSnakeCase(string input)
    {
        return string.Concat(
            input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString()))
            .ToLower();
    }
}