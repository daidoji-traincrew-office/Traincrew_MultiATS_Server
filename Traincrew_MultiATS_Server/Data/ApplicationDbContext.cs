using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Models;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Station> Stations { get; set; }
    public DbSet<InterlockingObject> InterlockingObjects { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<SwitchingMachine> SwitchingMachines { get; set; }
    public DbSet<TrackCircuit> TrackCircuits { get; set; }
    public DbSet<Lock> Locks { get; set; }
    public DbSet<LockCondition> LockConditions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Route>()
            .HasOne(r => r.RouteState)
            .WithOne(rs => rs.Route)
            .HasForeignKey<RouteState>(rs => rs.Id);
        modelBuilder.Entity<SwitchingMachine>()
            .HasOne(sm => sm.SwitchingMachineState)
            .WithOne(sms => sms.SwitchingMachine)
            .HasForeignKey<SwitchingMachineState>(sms => sms.Id);
        modelBuilder.Entity<TrackCircuit>()
            .HasOne(tc => tc.TrackCircuitState)
            .WithOne(tcs => tcs.TrackCircuit)
            .HasForeignKey<TrackCircuitState>(tcs => tcs.Id);
        modelBuilder.Entity<Lock>()
            .HasOne(l => l.LockCondition)
            .WithOne(lc => lc.Lock)
            .HasForeignKey<LockCondition>(lc => lc.LockId);
        modelBuilder.Entity<LockCondition>()
            .HasOne(lc => lc.targetObject)
            .WithMany(obj => obj.LockConditions)
            .HasForeignKey(lc => lc.ObjectId);
        
        // Convert all column names to snake_case 
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    private static string ToSnakeCase(string input)
    {
        return string.Concat(
                input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString()))
            .ToLower();
    }
}