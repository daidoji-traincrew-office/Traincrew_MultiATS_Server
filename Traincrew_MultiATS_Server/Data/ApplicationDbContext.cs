using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Models;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Station> Stations { get; set; }
    public DbSet<StationTimerState> StationTimerStates { get; set; }
    public DbSet<InterlockingObject> InterlockingObjects { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<RouteLockTrackCircuit> RouteLockTrackCircuits { get; set; }
    public DbSet<RouteState> RouteStates { get; set; }
    public DbSet<SwitchingMachine> SwitchingMachines { get; set; }
    public DbSet<TrackCircuit> TrackCircuits { get; set; }
    public DbSet<TrackCircuitState> TrackCircuitStates { get; set; }
    public DbSet<Lock> Locks { get; set; }
    public DbSet<LockCondition> LockConditions { get; set; }
    public DbSet<LockConditionObject> LockConditionObjects { get; set; }
    public DbSet<Signal> Signals { get; set; }
    public DbSet<SignalType> SignalTypes { get; set; }
    public DbSet<NextSignal> NextSignals { get; set; }
    public DbSet<TrackCircuitSignal> TrackCircuitSignals { get; set; }
    public DbSet<ProtectionZoneState> protectionZoneStates { get; set; }
    public DbSet<RouteLeverDestinationButton> RouteLeverDestinationButtons { get; set; }
    public DbSet<SwitchingMachineRoute> SwitchingMachineRoutes { get; set; }
    public DbSet<Lever> Levers { get; set; }
    public DbSet<DestinationButton> DestinationButtons { get; set; }
    public DbSet<DestinationButtonState> DestinationButtonStates { get; set; }
    public DbSet<SignalRoute> SignalRoutes { get; internal set; }
    public DbSet<ThrowOutControl> ThrowOutControls { get; set; }
    public DbSet<OperationNotificationDisplay> OperationNotificationDisplays { get; set; }
    public DbSet<OperationNotificationState> OperationNotificationStates { get; set; }
    public DbSet<DirectionRoute> DirectionRoutes { get; set; }
    public DbSet<DirectionSelfControlLever> DirectionSelfControlLevers { get; set; }
    public DbSet<TtcWindow> TtcWindows { get; set; }
    public DbSet<TtcWindowLink> TtcWindowLinks { get; set; }
    public DbSet<TtcWindowDisplayStation> TtcWindowDisplayStations { get; set; }
    public DbSet<TtcWindowTrackCircuit> TtcWindowTrackCircuits { get; set; }
    public DbSet<TtcWindowLinkRouteCondition> TtcWindowLinkRouteConditions { get; set; }
    public DbSet<TrainState> TrainStates { get; set; }
    public DbSet<TrainCarState> TrainCarStates { get; set; }
    public DbSet<TrainType> TrainTypes { get; set; }
    public DbSet<TrainDiagram> TrainDiagrams { get; set; }
    public DbSet<OperationInformationState> OperationInformationStates { get; set; }
    public DbSet<ServerState> ServerStates { get; set; }
    public DbSet<RouteCentralControlLever> RouteCentralControlLevers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StationTimerState>()
            .HasOne<Station>()
            .WithMany()
            .HasForeignKey(st => st.StationId)
            .HasPrincipalKey(s => s.Id);

        modelBuilder.Entity<Route>()
            .HasOne(r => r.RouteState)
            .WithOne()
            .HasForeignKey<RouteState>(rs => rs.Id);

        modelBuilder.Entity<Route>()
            .HasOne(r => r.Root)
            .WithMany()
            .HasForeignKey(r => r.RootId);

        modelBuilder.Entity<SwitchingMachine>()
            .HasOne(sm => sm.SwitchingMachineState)
            .WithOne(sms => sms.SwitchingMachine)
            .HasForeignKey<SwitchingMachineState>(sms => sms.Id);
        modelBuilder.Entity<TrackCircuit>()
            .HasOne(tc => tc.TrackCircuitState)
            .WithOne()
            .HasForeignKey<TrackCircuitState>(tcs => tcs.Id);
        modelBuilder.Entity<TrackCircuit>()
            .HasOne(tc => tc.OperationNotificationDisplay)
            .WithMany(ond => ond.TrackCircuits)
            .HasForeignKey(tc => tc.OperationNotificationDisplayName)
            .HasPrincipalKey(ond => ond.Name);
        modelBuilder.Entity<OperationNotificationDisplay>()
            .HasOne(ond => ond.OperationNotificationState)
            .WithOne()
            .HasForeignKey<OperationNotificationState>(ons => ons.DisplayName)
            .HasPrincipalKey<OperationNotificationDisplay>(ond => ond.Name);
        /*
        modelBuilder.Entity<Lock>()
            .HasOne(l => l.LockCondition)
            .WithOne(lc => lc.Lock)
            .HasForeignKey<LockCondition>(lc => lc.LockId);
        */
        modelBuilder.Entity<LockCondition>()
            .HasOne(lc => lc.Lock)
            .WithMany(l => l.LockConditions)
            .HasForeignKey(l => l.LockId)
            .HasPrincipalKey(l => l.Id);
        modelBuilder.Entity<LockCondition>()
            .HasOne(lc => lc.Parent)
            .WithMany()
            .HasForeignKey(lc => lc.ParentId)
            .HasPrincipalKey(lc => lc.Id);
        modelBuilder.Entity<LockConditionObject>()
            .HasOne(lco => lco.Object)
            .WithMany()
            .HasForeignKey(lco => lco.ObjectId)
            .HasPrincipalKey(o => o.Id);
        modelBuilder.Entity<Signal>()
            .HasOne(s => s.Type)
            .WithMany()
            .HasForeignKey(s => s.TypeName);
        modelBuilder.Entity<TrackCircuitSignal>()
            .HasOne(tcs => tcs.TrackCircuit)
            .WithMany()
            .HasForeignKey(tcs => tcs.TrackCircuitId)
            .HasPrincipalKey(tc => tc.Id);
        modelBuilder.Entity<TrackCircuitSignal>()
            .HasOne(tcs => tcs.Signal)
            .WithMany()
            .HasForeignKey(tcs => tcs.SignalName)
            .HasPrincipalKey(s => s.Name);
        modelBuilder.Entity<ProtectionZoneState>();
        modelBuilder.Entity<Lever>()
            .HasOne(l => l.LeverState)
            .WithOne()
            .HasForeignKey<LeverState>(ls => ls.Id);
        modelBuilder.Entity<DestinationButton>()
            .HasOne(db => db.DestinationButtonState)
            .WithOne()
            .HasForeignKey<DestinationButtonState>(dbs => dbs.Name);
        modelBuilder.Entity<ThrowOutControl>()
            .HasOne(tc => tc.Source)
            .WithMany()
            .HasForeignKey(tc => tc.SourceId)
            .HasPrincipalKey(io => io.Id);
        modelBuilder.Entity<ThrowOutControl>()
            .HasOne(tc => tc.Target)
            .WithMany()
            .HasForeignKey(tc => tc.TargetId)
            .HasPrincipalKey(io => io.Id);
        modelBuilder.Entity<ThrowOutControl>()
            .HasOne(tc => tc.ConditionLever)
            .WithMany()
            .HasForeignKey(tc => tc.ConditionLeverId)
            .HasPrincipalKey(l => l.Id);

        modelBuilder.Entity<DirectionRoute>()
            .HasOne(dl => dl.DirectionRouteState)
            .WithOne()
            .HasForeignKey<DirectionRouteState>(dls => dls.Id)
            .HasPrincipalKey<DirectionRoute>(dl => dl.Id);
        modelBuilder.Entity<DirectionRoute>()
            .HasOne(dr => dr.Lever)
            .WithOne()
            .HasForeignKey<DirectionRoute>(dl => dl.LeverId)
            .HasPrincipalKey<Lever>(l => l.Id);

        modelBuilder.Entity<DirectionSelfControlLever>()
            .HasOne(dsc => dsc.DirectionSelfControlLeverState)
            .WithOne()
            .HasForeignKey<DirectionSelfControlLeverState>(dscs => dscs.Id)
            .HasPrincipalKey<DirectionSelfControlLever>(dsc => dsc.Id);

        modelBuilder.Entity<DirectionSelfControlLever>();

        modelBuilder.Entity<TtcWindowLinkRouteCondition>()
            .HasOne(t => t.TtcWindowLink)
            .WithMany()
            .HasForeignKey(t => t.TtcWindowLinkId)
            .HasPrincipalKey(t => t.Id);

        modelBuilder.Entity<TtcWindowLinkRouteCondition>()
            .HasOne<Route>()
            .WithMany()
            .HasForeignKey(t => t.RouteId)
            .HasPrincipalKey(r => r.Id);

        modelBuilder.Entity<TtcWindow>()
            .HasOne(tw => tw.TtcWindowState)
            .WithOne()
            .HasForeignKey<TtcWindowState>(tws => tws.Name)
            .HasPrincipalKey<TtcWindow>(tw => tw.Name);

        modelBuilder.Entity<TrainCarState>()
            .HasKey(t => new { t.TrainStateId, t.Index });
        
        modelBuilder.Entity<RouteCentralControlLever>()
            .HasOne(rl => rl.RouteCentralControlLeverState)
            .WithOne()
            .HasForeignKey<RouteCentralControlLeverState>(rls => rls.Id)
            .HasPrincipalKey<RouteCentralControlLever>(rl => rl.Id);

        // Convert all column names to snake_case 
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.ClrType.Name == nameof(SignalIndication))
            {
                continue;
            }

            foreach (var property in entity.GetProperties())
            {
                var columnAttribute = property.GetAnnotations()
                    .FirstOrDefault(a => a.Name == "Relational:ColumnName");
                if (columnAttribute != null)
                {
                    continue;
                }

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