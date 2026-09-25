using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.UT.Models;

/// <summary>
/// <see cref="InterlockingObject"/> 派生型の CloneWithState のテスト。
/// マスタのスナップショットはプロセス全体で共有されるため、CloneWithState は
/// 元インスタンスを汚染せず、別インスタンスにのみ状態を載せて返す必要がある。
/// </summary>
public class InterlockingObjectCloneWithStateTest
{
    [Fact]
    public void TrackCircuit_CloneWithState_CopiesMasterAndAttachesState()
    {
        var original = new TrackCircuit
        {
            Id = 1,
            Name = "TC1",
            Type = ObjectType.TrackCircuit,
            StationId = "TH71",
            ProtectionZone = 3
        };
        var state = new TrackCircuitState { Id = 1, TrainNumber = "1234" };

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.StationId, clone.StationId);
        Assert.Equal(original.ProtectionZone, clone.ProtectionZone);
        Assert.Null(original.TrackCircuitState);
        Assert.Same(state, clone.TrackCircuitState);
    }

    [Fact]
    public void Route_CloneWithState_CopiesMasterAndAttachesState()
    {
        var original = new Route
        {
            Id = 2,
            Name = "R1",
            Type = ObjectType.Route,
            StationId = "TH71",
            TcName = "tc_R1",
            RouteType = RouteType.Arriving
        };
        var state = new RouteState { Id = 2, IsRouteSecured = RaiseDrop.Raise };

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.StationId, clone.StationId);
        Assert.Equal(original.TcName, clone.TcName);
        Assert.Equal(original.RouteType, clone.RouteType);
        Assert.Null(original.RouteState);
        Assert.Same(state, clone.RouteState);
    }

    [Fact]
    public void Route_CloneWithState_AcceptsNull()
    {
        var original = new Route
        {
            Id = 2,
            Name = "R1",
            Type = ObjectType.Route,
            TcName = "tc_R1",
            RouteType = RouteType.Arriving,
            RouteState = new RouteState { Id = 2 }
        };

        var clone = original.CloneWithState(null);

        Assert.NotSame(original, clone);
        Assert.NotNull(original.RouteState);
        Assert.Null(clone.RouteState);
    }

    [Fact]
    public void SwitchingMachine_CloneWithState_CopiesMasterAndAttachesState()
    {
        var original = new SwitchingMachine
        {
            Id = 3,
            Name = "SM1",
            Type = ObjectType.SwitchingMachine,
            StationId = "TH71",
            TcName = "tc_SM1"
        };
        var state = new SwitchingMachineState { Id = 3, IsReverse = NR.Reversed };

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.StationId, clone.StationId);
        Assert.Equal(original.TcName, clone.TcName);
        Assert.Null(original.SwitchingMachineState);
        Assert.Same(state, clone.SwitchingMachineState);
    }

    [Fact]
    public void Lever_CloneWithState_CopiesMasterAndAttachesState()
    {
        var original = new Lever
        {
            Id = 4,
            Name = "L1",
            Type = ObjectType.Lever,
            StationId = "TH71",
            LeverType = LeverType.Route,
            SwitchingMachineId = 3
        };
        var state = new LeverState { Id = 4, IsReversed = LCR.Right };

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.StationId, clone.StationId);
        Assert.Equal(original.LeverType, clone.LeverType);
        Assert.Equal(original.SwitchingMachineId, clone.SwitchingMachineId);
        Assert.Null(original.LeverState);
        Assert.Same(state, clone.LeverState);
    }

    [Fact]
    public void DirectionRoute_CloneWithState_CopiesMasterAndAttachesState()
    {
        var original = new DirectionRoute
        {
            Id = 5,
            Name = "DR1",
            Type = ObjectType.DirectionRoute,
            StationId = "TH71",
            LeverId = 4,
            DirectionSelfControlLeverId = 6
        };
        var state = new DirectionRouteState { Id = 5, isLr = LR.Right };

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.StationId, clone.StationId);
        Assert.Equal(original.LeverId, clone.LeverId);
        Assert.Equal(original.DirectionSelfControlLeverId, clone.DirectionSelfControlLeverId);
        Assert.Null(original.DirectionRouteState);
        Assert.Same(state, clone.DirectionRouteState);
    }

    [Fact]
    public void DirectionRoute_CloneWithState_AcceptsNull()
    {
        var original = new DirectionRoute
        {
            Id = 5,
            Name = "DR1",
            Type = ObjectType.DirectionRoute,
            LeverId = 4,
            DirectionRouteState = new DirectionRouteState { Id = 5 }
        };

        var clone = original.CloneWithState(null);

        Assert.NotSame(original, clone);
        Assert.NotNull(original.DirectionRouteState);
        Assert.Null(clone.DirectionRouteState);
    }

    [Fact]
    public void DirectionSelfControlLever_CloneWithState_CopiesMasterAndAttachesState()
    {
        var original = new DirectionSelfControlLever
        {
            Id = 6,
            Name = "DSCL1",
            Type = ObjectType.DirectionSelfControlLever,
            StationId = "TH71"
        };
        var state = new DirectionSelfControlLeverState { Id = 6, IsInsertedKey = true, IsReversed = NR.Reversed };

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.StationId, clone.StationId);
        Assert.Null(original.DirectionSelfControlLeverState);
        Assert.Same(state, clone.DirectionSelfControlLeverState);
    }

    [Fact]
    public void DirectionSelfControlLever_CloneWithState_AcceptsNull()
    {
        var original = new DirectionSelfControlLever
        {
            Id = 6,
            Name = "DSCL1",
            Type = ObjectType.DirectionSelfControlLever,
            DirectionSelfControlLeverState = new DirectionSelfControlLeverState { Id = 6 }
        };

        var clone = original.CloneWithState(null);

        Assert.NotSame(original, clone);
        Assert.NotNull(original.DirectionSelfControlLeverState);
        Assert.Null(clone.DirectionSelfControlLeverState);
    }

    [Fact]
    public void RouteCentralControlLever_CloneWithState_CopiesMasterAndAttachesState()
    {
        var original = new RouteCentralControlLever
        {
            Id = 7,
            Name = "RCCL1",
            Type = ObjectType.RouteCentralControlLever,
            StationId = "TH71"
        };
        var state = new RouteCentralControlLeverState { Id = 7, IsCenterControlled = true };

        var clone = original.CloneWithState(state);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.StationId, clone.StationId);
        Assert.Null(original.RouteCentralControlLeverState);
        Assert.Same(state, clone.RouteCentralControlLeverState);
    }

    [Fact]
    public void RouteCentralControlLever_CloneWithState_AcceptsNull()
    {
        var original = new RouteCentralControlLever
        {
            Id = 7,
            Name = "RCCL1",
            Type = ObjectType.RouteCentralControlLever,
            RouteCentralControlLeverState = new RouteCentralControlLeverState { Id = 7 }
        };

        var clone = original.CloneWithState(null);

        Assert.NotSame(original, clone);
        Assert.NotNull(original.RouteCentralControlLeverState);
        Assert.Null(clone.RouteCentralControlLeverState);
    }
}
