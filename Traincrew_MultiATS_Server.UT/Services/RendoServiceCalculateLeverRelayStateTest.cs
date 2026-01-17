using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Services;

/// <summary>
/// RendoService.CalculateLeverRelayState メソッドのユニットテスト
/// </summary>
public class RendoServiceCalculateLeverRelayStateTest
{
    // テストヘルパーメソッド: CalculateLeverRelayStateを呼び出すためのラッパー
    private static LeverRelayState CallCalculateLeverRelayState(
        Route route,
        RouteLeverDestinationButton routeLeverDestinationButton,
        Lever? lever,
        DestinationButton? button,
        RouteCentralControlLever? routeCentralControlLever,
        List<ThrowOutControl> sourceThrowOutControls,
        List<ThrowOutControl> targetThrowOutControls,
        List<LockCondition> lockCondition,
        List<LockCondition> signalControlConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjectById,
        Dictionary<ulong, Route> routeById,
        Dictionary<ulong, Lever> leverByRouteId,
        Dictionary<ulong, List<Route>> routesByLeverId,
        Dictionary<ulong, List<ThrowOutControl>> allSourceThrowOutControls)
    {
        // publicメソッドを直接呼び出す
        var rendoService = new RendoService(
            null!, null!, null!, null!, null!, null!, null!, null!, null!, null!,
            null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        return rendoService.CalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            routeCentralControlLever,
            sourceThrowOutControls,
            targetThrowOutControls,
            lockCondition,
            signalControlConditions,
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            allSourceThrowOutControls
        );
    }

    // テストデータビルダー: デフォルト値を持つオブジェクトを生成
    private static Route CreateRoute(ulong id, RouteState? routeState = null, ulong? approachLockFinalTrackCircuitId = null)
    {
        return new()
        {
            Id = id,
            StationId = "TEST",
            Name = $"Route{id}",
            TcName = $"TC{id}",
            RouteType = RouteType.Departure,
            RouteState = routeState ?? new RouteState
            {
                Id = id,
                IsLeverRelayRaised = RaiseDrop.Drop,
                IsCtcRelayRaised = RaiseDrop.Drop,
                IsThrowOutXRelayRaised = RaiseDrop.Drop,
                IsThrowOutXRRelayRaised = RaiseDrop.Drop,
                IsThrowOutYSRelayRaised = RaiseDrop.Drop,
                IsRouteLockRaised = RaiseDrop.Drop,
                IsSignalControlRaised = RaiseDrop.Drop,
                IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
            },
            ApproachLockFinalTrackCircuitId = approachLockFinalTrackCircuitId
        };
    }

    private static Lever CreateLever(ulong id, LCR isReversed)
    {
        return new()
        {
            Id = id,
            StationId = "TEST",
            Name = $"Lever{id}",
            LeverType = LeverType.Route,
            LeverState = new()
            {
                Id = id,
                IsReversed = isReversed
            }
        };
    }

    private static DestinationButton CreateButton(string name, RaiseDrop isRaised)
    {
        return new()
        {
            Name = name,
            StationId = "TEST",
            DestinationButtonState = new()
            {
                Name = name,
                IsRaised = isRaised,
                OperatedAt = DateTime.UtcNow
            }
        };
    }

    private static RouteLeverDestinationButton CreateRouteLeverDestinationButton(
        ulong routeId, ulong leverId, LR direction, string? buttonName = null)
    {
        return new()
        {
            RouteId = routeId,
            LeverId = leverId,
            Direction = direction,
            DestinationButtonName = buttonName
        };
    }

    private static TrackCircuit CreateTrackCircuit(ulong id, bool isLocked)
    {
        return new()
        {
            Id = id,
            StationId = "TEST",
            Name = $"TC{id}",
            TrackCircuitState = new()
            {
                Id = id,
                TrainNumber = "",
                IsLocked = isLocked,
                IsShortCircuit = false
            }
        };
    }

    #region 1. 鎖錠条件のテスト

    [Fact(DisplayName = "鎖錠条件を満たさない場合_どんな状況でもてこ反応リレーが落下すること")]
    public void LeverRelayDrops_WhenLockConditionsNotSatisfied()
    {
        // Arrange: てこを倒して着点も押されている状況だが、鎖錠条件を満たさない
        const ulong routeId = 1ul;
        const ulong leverId = 10ul;
        const ulong routeLockingId = 100ul;

        var route = CreateRoute(routeId);
        var lever = CreateLever(leverId, LCR.Left);
        var button = CreateButton("P1", RaiseDrop.Raise);
        var routeLeverDestinationButton = CreateRouteLeverDestinationButton(routeId, leverId, LR.Left, "P1");

        // 鎖錠条件: lockObjectIdのオブジェクトが特定の状態である必要がある
        var lockObject = new Route
        {
            Id = routeLockingId,
            StationId = "TEST",
            Name = "LockRoute",
            TcName = "TC100",
            RouteType = RouteType.Departure,
            RouteState = new()
            {
                Id = routeLockingId,
                IsLeverRelayRaised = RaiseDrop.Raise // 鎖錠条件を満たさない
            }
        };

        var lockCondition = new List<LockCondition>
        {
            new LockConditionObject
            {
                ObjectId = routeLockingId,
                IsReverse = NR.Normal, // 定位鎖錠をかける
                Type = LockConditionType.Object
            }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { routeId, route },
            { leverId, lever },
            { routeLockingId, lockObject }
        };

        var routeById = new Dictionary<ulong, Route> { { routeId, route }, { routeLockingId, lockObject } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { routeId, lever } };
        var routesByLeverId = new Dictionary<ulong, List<Route>> { { leverId, [route] } };

        // Act
        var result = CallCalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            null,
            [],
            [],
            lockCondition,
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(RaiseDrop.Drop, result.IsLeverRelayRaised);
    }

    #endregion

    #region 2. 基本的な進路(総括制御なし、駅扱い)

    [Fact(DisplayName = "基本進路_てこを倒してもてこ反応リレーは落下のまま")]
    public void LeverRelayStaysDropped_WhenOnlyLeverReversed()
    {
        // Arrange
        var routeId = 1ul;
        var leverId = 10ul;

        var route = CreateRoute(routeId);
        var lever = CreateLever(leverId, LCR.Left); // てこを倒している
        var button = CreateButton("P1", RaiseDrop.Drop); // ボタンは押されていない
        var routeLeverDestinationButton = CreateRouteLeverDestinationButton(routeId, leverId, LR.Left, "P1");

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { routeId, route },
            { leverId, lever }
        };

        var routeById = new Dictionary<ulong, Route> { { routeId, route } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { routeId, lever } };
        var routesByLeverId = new Dictionary<ulong, List<Route>> { { leverId, [route] } };

        // Act
        var result = CallCalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            null,
            [],
            [],
            [], // 鎖錠条件なし
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(RaiseDrop.Drop, result.IsLeverRelayRaised);
        // 総括制御として取っていないので、X, XR, YSリレーは落下
        Assert.Equal(RaiseDrop.Drop, result.IsXRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutYSRelayRaised);
    }

    [Fact(DisplayName = "基本進路_てこを倒して着点を押すとてこ反応リレーが扛上")]
    public void LeverRelayRaises_WhenLeverReversedAndButtonPressed()
    {
        // Arrange
        var routeId = 1ul;
        var leverId = 10ul;

        var route = CreateRoute(routeId);
        var lever = CreateLever(leverId, LCR.Left);
        var button = CreateButton("P1", RaiseDrop.Raise); // ボタンを押す
        var routeLeverDestinationButton = CreateRouteLeverDestinationButton(routeId, leverId, LR.Left, "P1");

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { routeId, route },
            { leverId, lever }
        };

        var routeById = new Dictionary<ulong, Route> { { routeId, route } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { routeId, lever } };
        var routesByLeverId = new Dictionary<ulong, List<Route>> { { leverId, [route] } };

        // Act
        var result = CallCalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            null,
            [],
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(RaiseDrop.Raise, result.IsLeverRelayRaised);
        // 総括制御として取っていないので、X, XR, YSリレーは落下
        Assert.Equal(RaiseDrop.Drop, result.IsXRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutYSRelayRaised);
    }

    [Fact(DisplayName = "基本進路_てこを戻さない限りてこ反応リレーが扛上し続ける")]
    public void LeverRelayStaysRaised_WhileLeverNotReturned()
    {
        // Arrange
        var routeId = 1ul;
        var leverId = 10ul;

        var routeState = new RouteState
        {
            Id = routeId,
            IsLeverRelayRaised = RaiseDrop.Raise, // 既に扛上している
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route = CreateRoute(routeId, routeState);
        var lever = CreateLever(leverId, LCR.Left); // てこは倒したまま
        var button = CreateButton("P1", RaiseDrop.Drop); // ボタンは離されている
        var routeLeverDestinationButton = CreateRouteLeverDestinationButton(routeId, leverId, LR.Left, "P1");

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { routeId, route },
            { leverId, lever }
        };

        var routeById = new Dictionary<ulong, Route> { { routeId, route } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { routeId, lever } };
        var routesByLeverId = new Dictionary<ulong, List<Route>> { { leverId, [route] } };

        // Act
        var result = CallCalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            null,
            [],
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(RaiseDrop.Raise, result.IsLeverRelayRaised);
        // 総括制御として取っていないので、X, XR, YSリレーは落下
        Assert.Equal(RaiseDrop.Drop, result.IsXRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutYSRelayRaised);
    }

    [Fact(DisplayName = "基本進路_てこを戻すとてこ反応リレーが落下")]
    public void LeverRelayDrops_WhenLeverReturned()
    {
        // Arrange
        var routeId = 1ul;
        var leverId = 10ul;

        var routeState = new RouteState
        {
            Id = routeId,
            IsLeverRelayRaised = RaiseDrop.Raise, // 既に扛上している
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route = CreateRoute(routeId, routeState);
        var lever = CreateLever(leverId, LCR.Center); // てこを中立に戻す
        var button = CreateButton("P1", RaiseDrop.Drop);
        var routeLeverDestinationButton = CreateRouteLeverDestinationButton(routeId, leverId, LR.Left, "P1");

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { routeId, route },
            { leverId, lever }
        };

        var routeById = new Dictionary<ulong, Route> { { routeId, route } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { routeId, lever } };
        var routesByLeverId = new Dictionary<ulong, List<Route>> { { leverId, [route] } };

        // Act
        var result = CallCalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            null,
            [],
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(RaiseDrop.Drop, result.IsLeverRelayRaised);
        // 総括制御として取っていないので、X, XR, YSリレーは落下
        Assert.Equal(RaiseDrop.Drop, result.IsXRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutYSRelayRaised);
    }

    #endregion

    #region 3. CTC(集中扱い)

    [Theory(DisplayName = "CTC_集中扱い時_てこ反応リレーがCTCリレーの状態と一致")]
    [InlineData(RaiseDrop.Raise, RaiseDrop.Raise)]
    [InlineData(RaiseDrop.Drop, RaiseDrop.Drop)]
    public void LeverRelayMatchesCtcRelay_WhenInCentralControl(RaiseDrop ctcRelayState, RaiseDrop expectedLeverRelay)
    {
        // Arrange
        var routeId = 1ul;
        var leverId = 10ul;

        var routeState = new RouteState
        {
            Id = routeId,
            IsLeverRelayRaised = RaiseDrop.Drop,
            IsCtcRelayRaised = ctcRelayState,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route = CreateRoute(routeId, routeState);
        var lever = CreateLever(leverId, LCR.Center); // てこは中立
        var button = CreateButton("P1", RaiseDrop.Drop); // ボタンも押されていない
        var routeLeverDestinationButton = CreateRouteLeverDestinationButton(routeId, leverId, LR.Left, "P1");

        // 集中扱い
        var routeCentralControlLever = new RouteCentralControlLever
        {
            Id = routeId,
            StationId = "TEST",
            Name = $"RCCL{routeId}",
            RouteCentralControlLeverState = new()
            {
                Id = routeId,
                IsChrRelayRaised = RaiseDrop.Raise // CHRリレー扛上 = 集中扱い
            }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { routeId, route },
            { leverId, lever }
        };

        var routeById = new Dictionary<ulong, Route> { { routeId, route } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { routeId, lever } };
        var routesByLeverId = new Dictionary<ulong, List<Route>> { { leverId, [route] } };

        // Act
        var result = CallCalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            routeCentralControlLever,
            [],
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(expectedLeverRelay, result.IsLeverRelayRaised);
        // 総括制御として取っていないので、X, XR, YSリレーは落下
        Assert.Equal(RaiseDrop.Drop, result.IsXRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutYSRelayRaised);
    }

    [Fact(DisplayName = "CTC_集中扱いから駅扱いに切り替えるとてこ反応リレーが落下")]
    public void LeverRelayDrops_WhenSwitchingFromCentralToStationControl()
    {
        // Arrange
        var routeId = 1ul;
        var leverId = 10ul;

        var routeState = new RouteState
        {
            Id = routeId,
            IsLeverRelayRaised = RaiseDrop.Raise, // 集中扱い時に扛上していた
            IsCtcRelayRaised = RaiseDrop.Raise,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route = CreateRoute(routeId, routeState);
        var lever = CreateLever(leverId, LCR.Center); // てこは中立
        var button = CreateButton("P1", RaiseDrop.Drop);
        var routeLeverDestinationButton = CreateRouteLeverDestinationButton(routeId, leverId, LR.Left, "P1");

        // 駅扱い(CHRリレー落下)
        var routeCentralControlLever = new RouteCentralControlLever
        {
            Id = routeId,
            StationId = "TEST",
            Name = $"RCCL{routeId}",
            RouteCentralControlLeverState = new()
            {
                Id = routeId,
                IsChrRelayRaised = RaiseDrop.Drop // CHRリレー落下 = 駅扱い
            }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { routeId, route },
            { leverId, lever }
        };

        var routeById = new Dictionary<ulong, Route> { { routeId, route } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { routeId, lever } };
        var routesByLeverId = new Dictionary<ulong, List<Route>> { { leverId, [route] } };

        // Act
        var result = CallCalculateLeverRelayState(
            route,
            routeLeverDestinationButton,
            lever,
            button,
            routeCentralControlLever,
            [],
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(RaiseDrop.Drop, result.IsLeverRelayRaised);
        // 総括制御として取っていないので、X, XR, YSリレーは落下
        Assert.Equal(RaiseDrop.Drop, result.IsXRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result.IsThrowOutYSRelayRaised);
    }

    #endregion

    #region 4. てこアリ総括制御

    [Theory(DisplayName = "てこアリ総括_それぞれのてこと着点を押してそれぞれのてこ反応リレーが扛上")]
    [InlineData(1ul, 10ul, LR.Left, LCR.Left, "P1", 2ul, 20ul, LR.Right, LCR.Right, "P2")]
    [InlineData(3ul, 30ul, LR.Right, LCR.Right, "P3", 4ul, 40ul, LR.Left, LCR.Left, "P4")]
    public void EachLeverRelayRaises_WhenEachLeverAndButtonPressed_WithLeverThrowOutControl(
        ulong route1Id, ulong lever1Id, LR direction1, LCR leverState1, string button1Name,
        ulong route2Id, ulong lever2Id, LR direction2, LCR leverState2, string button2Name)
    {
        // Arrange: 2つの進路が総括制御の関係にある
        var route1 = CreateRoute(route1Id);
        var lever1 = CreateLever(lever1Id, leverState1);
        var button1 = CreateButton(button1Name, RaiseDrop.Raise);
        var routeLeverDestinationButton1 = CreateRouteLeverDestinationButton(route1Id, lever1Id, direction1, button1Name);

        var route2 = CreateRoute(route2Id);
        var lever2 = CreateLever(lever2Id, leverState2);
        CreateButton(button2Name, RaiseDrop.Raise);

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1 },
            { route2Id, route2 },
            { lever2Id, lever2 }
        };

        var routeById = new Dictionary<ulong, Route> { { route1Id, route1 }, { route2Id, route2 } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { route1Id, lever1 }, { route2Id, lever2 } };
        var routesByLeverId = new Dictionary<ulong, List<Route>>
        {
            { lever1Id, [route1] },
            { lever2Id, [route2] }
        };

        // 総括制御関係なし (各進路は独立)
        var sourceThrowOutControls = new List<ThrowOutControl>();

        // Act
        var result1 = CallCalculateLeverRelayState(
            route1,
            routeLeverDestinationButton1,
            lever1,
            button1,
            null,
            sourceThrowOutControls,
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            new()
        );

        // Assert
        Assert.Equal(RaiseDrop.Raise, result1.IsLeverRelayRaised);
        // 総括制御関係なし(独立した進路)なので、X, XR, YSリレーは落下
        Assert.Equal(RaiseDrop.Drop, result1.IsXRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result1.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Drop, result1.IsThrowOutYSRelayRaised);
    }

    [Fact(DisplayName = "てこアリ総括_てこを戻してそれぞれ落下_お互い影響を与えない")]
    public void EachLeverRelayDropsIndependently_WhenLeverReturned()
    {
        // Arrange: route1が制御元、route2が制御先の総括制御関係がある
        // 制御先のてこを戻しても制御元に影響なし、制御元のてこを戻しても制御先に影響なし
        var route1Id = 1ul;
        var lever1Id = 10ul;
        var route2Id = 2ul;
        var lever2Id = 20ul;

        var routeState1 = new RouteState
        {
            Id = route1Id,
            IsLeverRelayRaised = RaiseDrop.Drop, // 制御元は落下
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var routeState2 = new RouteState
        {
            Id = route2Id,
            IsLeverRelayRaised = RaiseDrop.Raise, // 制御先は扛上中
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Raise,
            IsThrowOutYSRelayRaised = RaiseDrop.Raise,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route1 = CreateRoute(route1Id, routeState1);
        var lever1 = CreateLever(lever1Id, LCR.Left); // 制御元のてこは倒れている
        var button1 = CreateButton("P1", RaiseDrop.Drop);
        var routeLeverDestinationButton1 = CreateRouteLeverDestinationButton(route1Id, lever1Id, LR.Left, "P1");

        var route2 = CreateRoute(route2Id, routeState2);
        var lever2 = CreateLever(lever2Id, LCR.Left); // 制御先のてこも倒れている
        CreateButton("P2", RaiseDrop.Drop);
        var routeLeverDestinationButton2 = CreateRouteLeverDestinationButton(route2Id, lever2Id, LR.Left, "P2");

        // 総括制御: route1 -> route2
        var sourceThrowOutControls = new List<ThrowOutControl>
        {
            new()
            {
                SourceId = route1Id,
                TargetId = route2Id,
                ControlType = ThrowOutControlType.WithLever
            }
        };

        var routeById = new Dictionary<ulong, Route> { { route1Id, route1 }, { route2Id, route2 } };
        var routesByLeverId = new Dictionary<ulong, List<Route>>
        {
            { lever1Id, [route1] },
            { lever2Id, [route2] }
        };

        var allSourceThrowOutControls = new Dictionary<ulong, List<ThrowOutControl>>
        {
            { route2Id, sourceThrowOutControls }
        };

        // Act: 制御先のてこを戻した場合
        var lever2Returned = CreateLever(lever2Id, LCR.Center);
        var button2Released = CreateButton("P2", RaiseDrop.Drop);
        var interlockingObjectById2 = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1 },
            { route2Id, route2 },
            { lever2Id, lever2Returned }
        };
        var leverByRouteId2 = new Dictionary<ulong, Lever> { { route1Id, lever1 }, { route2Id, lever2Returned } };

        var result2 = CallCalculateLeverRelayState(
            route2,
            routeLeverDestinationButton2,
            lever2Returned,
            button2Released,
            null,
            sourceThrowOutControls,
            [],
            [],
            [],
            interlockingObjectById2,
            routeById,
            leverByRouteId2,
            routesByLeverId,
            allSourceThrowOutControls
        );

        // Act: 制御元のてこを戻した場合
        var lever1Returned = CreateLever(lever1Id, LCR.Center);
        var interlockingObjectById3 = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1Returned },
            { route2Id, route2 },
            { lever2Id, lever2 }
        };
        var leverByRouteId3 = new Dictionary<ulong, Lever> { { route1Id, lever1Returned }, { route2Id, lever2 } };

        var result1 = CallCalculateLeverRelayState(
            route1,
            routeLeverDestinationButton1,
            lever1Returned,
            button1,
            null,
            [],
            sourceThrowOutControls,
            [],
            [],
            interlockingObjectById3,
            routeById,
            leverByRouteId3,
            routesByLeverId,
            allSourceThrowOutControls
        );

        // Assert: 制御先のてこを戻してもYSが扛上しているため、てこ反も扛上のまま
        // てこが中立でも、YS扛上の条件でてこ反が扛上
        Assert.Equal(RaiseDrop.Raise, result2.IsLeverRelayRaised);
        Assert.Equal(RaiseDrop.Raise, result2.IsThrowOutXRRelayRaised); // 自己保持
        Assert.Equal(RaiseDrop.Raise, result2.IsThrowOutYSRelayRaised); // 接近鎖錠条件を満たすのでYSも扛上
        // 制御元は変わらず落下のまま
        Assert.Equal(RaiseDrop.Drop, routeState1.IsLeverRelayRaised);

        // Assert: 制御元を戻しても制御元だけ落下、制御先は変わらず扛上
        Assert.Equal(RaiseDrop.Drop, result1.IsLeverRelayRaised);
        Assert.Equal(RaiseDrop.Raise, routeState2.IsLeverRelayRaised);
        Assert.Equal(RaiseDrop.Raise, routeState2.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Raise, routeState2.IsThrowOutYSRelayRaised);
    }

    [Fact(DisplayName = "てこアリ総括_制御元のてこを倒すと制御先のXR,YSを扛上させる")]
    public void XRAndYSRelayIsRaised_WhenSourceLeverReversed()
    {
        // Arrange: route1が制御元、route2が制御先
        // 制御元のてこのみを倒すと、XR/YSが扛上
        var route1Id = 1ul;
        var lever1Id = 10ul;
        var route2Id = 2ul;
        var lever2Id = 20ul;
        var tcId = 100ul;

        var routeState1 = new RouteState
        {
            Id = route1Id,
            IsLeverRelayRaised = RaiseDrop.Drop, // 初期状態は落下
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var routeState2 = new RouteState
        {
            Id = route2Id,
            IsLeverRelayRaised = RaiseDrop.Drop,
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var tc = CreateTrackCircuit(tcId, false); // 接近鎖錠最終軌道回路が鎖錠していない

        var route1 = CreateRoute(route1Id, routeState1);
        var lever1 = CreateLever(lever1Id, LCR.Left); // 制御元のてこを倒す
        var button1 = CreateButton("P1", RaiseDrop.Drop);
        var routeLeverDestinationButton1 = CreateRouteLeverDestinationButton(route1Id, lever1Id, LR.Left, "P1");

        var route2 = CreateRoute(route2Id, routeState2, tcId); // 接近鎖錠条件を設定
        var lever2 = CreateLever(lever2Id, LCR.Center); // 制御先のてこは中立
        var button2 = CreateButton("P2", RaiseDrop.Drop);
        var routeLeverDestinationButton2 = CreateRouteLeverDestinationButton(route2Id, lever2Id, LR.Left, "P2");

        var sourceThrowOutControls = new List<ThrowOutControl>
        {
            new()
            {
                SourceId = route1Id,
                TargetId = route2Id,
                ControlType = ThrowOutControlType.WithLever
            }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1 },
            { route2Id, route2 },
            { lever2Id, lever2 },
            { tcId, tc }
        };

        var routeById = new Dictionary<ulong, Route> { { route1Id, route1 }, { route2Id, route2 } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { route1Id, lever1 }, { route2Id, lever2 } };
        var routesByLeverId = new Dictionary<ulong, List<Route>>
        {
            { lever1Id, [route1] },
            { lever2Id, [route2] }
        };

        var allSourceThrowOutControls = new Dictionary<ulong, List<ThrowOutControl>>
        {
            { route2Id, sourceThrowOutControls }
        };

        // Act: まず制御元を計算
        var result1 = CallCalculateLeverRelayState(
            route1,
            routeLeverDestinationButton1,
            lever1,
            button1,
            null,
            [],
            sourceThrowOutControls,
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            allSourceThrowOutControls
        );

        // 制御元のリレー状態を更新
        routeState1.IsLeverRelayRaised = result1.IsLeverRelayRaised;

        // Act: 制御先の計算
        var result2 = CallCalculateLeverRelayState(
            route2,
            routeLeverDestinationButton2,
            lever2,
            button2,
            null,
            sourceThrowOutControls,
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            allSourceThrowOutControls
        );

        // Assert: 制御先のXRとYSリレーが両方扛上
        Assert.Equal(RaiseDrop.Raise, result2.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Raise, result2.IsThrowOutYSRelayRaised);
    }

    [Fact(DisplayName = "てこアリ総括_制御元のてこと制御先のボタンを押して進路が取れる")]
    public void RouteEstablished_WhenSourceLeverAndTargetButtonPressed()
    {
        // Arrange: route1が制御元、route2が制御先
        var route1Id = 1ul;
        var lever1Id = 10ul;
        var route2Id = 2ul;
        var lever2Id = 20ul;
        var tcId = 100ul;

        var routeState1 = new RouteState
        {
            Id = route1Id,
            IsLeverRelayRaised = RaiseDrop.Drop,
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var routeState2 = new RouteState
        {
            Id = route2Id,
            IsLeverRelayRaised = RaiseDrop.Drop,
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var tc = CreateTrackCircuit(tcId, false); // 接近鎖錠最終軌道回路が鎖錠していない

        var route1 = CreateRoute(route1Id, routeState1);
        var lever1 = CreateLever(lever1Id, LCR.Left); // 制御元のてこを倒す
        var button1 = CreateButton("P1", RaiseDrop.Drop); // 制御元のボタンは押さない
        var routeLeverDestinationButton1 = CreateRouteLeverDestinationButton(route1Id, lever1Id, LR.Left, "P1");

        var route2 = CreateRoute(route2Id, routeState2, tcId); // 接近鎖錠条件を設定
        var lever2 = CreateLever(lever2Id, LCR.Center); // 制御先のてこは中立
        var button2 = CreateButton("P2", RaiseDrop.Raise); // 制御先のボタンを押す
        var routeLeverDestinationButton2 = CreateRouteLeverDestinationButton(route2Id, lever2Id, LR.Left, "P2");

        var sourceThrowOutControls = new List<ThrowOutControl>
        {
            new()
            {
                SourceId = route1Id,
                TargetId = route2Id,
                ControlType = ThrowOutControlType.WithLever
            }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1 },
            { route2Id, route2 },
            { lever2Id, lever2 },
            { tcId, tc }
        };

        var routeById = new Dictionary<ulong, Route> { { route1Id, route1 }, { route2Id, route2 } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { route1Id, lever1 }, { route2Id, lever2 } };
        var routesByLeverId = new Dictionary<ulong, List<Route>>
        {
            { lever1Id, [route1] },
            { lever2Id, [route2] }
        };

        var allSourceThrowOutControls = new Dictionary<ulong, List<ThrowOutControl>>
        {
            { route2Id, sourceThrowOutControls }
        };

        // Act: 制御先の計算
        var result2 = CallCalculateLeverRelayState(
            route2,
            routeLeverDestinationButton2,
            lever2,
            button2,
            null,
            sourceThrowOutControls,
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            allSourceThrowOutControls
        );

        // Assert: 制御先のてこ反応リレー、XR/YSが扛上する
        Assert.Equal(RaiseDrop.Raise, result2.IsLeverRelayRaised);
        Assert.Equal(RaiseDrop.Raise, result2.IsThrowOutXRRelayRaised);
        Assert.Equal(RaiseDrop.Raise, result2.IsThrowOutYSRelayRaised);
        // 制御先のリレー状態を更新して制御元の計算に使用
        routeState2.IsLeverRelayRaised = result2.IsLeverRelayRaised;
        routeState2.IsThrowOutXRRelayRaised = result2.IsThrowOutXRRelayRaised;
        routeState2.IsThrowOutYSRelayRaised = result2.IsThrowOutYSRelayRaised;

        // Act: 制御元の計算
        var result1 = CallCalculateLeverRelayState(
            route1,
            routeLeverDestinationButton1,
            lever1,
            button1,
            null,
            [],
            sourceThrowOutControls,
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            allSourceThrowOutControls
        );

        // 制御元のリレー状態を更新して制御先の計算に使用
        routeState1.IsLeverRelayRaised = result1.IsLeverRelayRaised;

        // Assert: 制御元のてこ反応リレーが扛上
        Assert.Equal(RaiseDrop.Raise, result1.IsLeverRelayRaised);
    }

    #endregion

    #region 5. てこなし総括制御

    [Fact(DisplayName = "てこなし総括_制御元のてこ反応リレー扛上かつ接近鎖錠条件を満たす時_Xリレーが扛上")]
    public void XRelayRaises_WhenSourceLeverRelayRaisedAndApproachLockSatisfied()
    {
        // Arrange: route1が制御元、route2が制御先(てこなし)
        var route1Id = 1ul;
        var lever1Id = 10ul;
        var route2Id = 2ul;
        var tcId = 100ul;

        var routeState1 = new RouteState
        {
            Id = route1Id,
            IsLeverRelayRaised = RaiseDrop.Raise, // 制御元が扛上
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var routeState2 = new RouteState
        {
            Id = route2Id,
            IsLeverRelayRaised = RaiseDrop.Drop,
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route1 = CreateRoute(route1Id, routeState1);
        var lever1 = CreateLever(lever1Id, LCR.Left);

        var tc = CreateTrackCircuit(tcId, false); // 接近鎖錠最終軌道回路が鎖錠していない
        var route2 = CreateRoute(route2Id, routeState2, tcId);

        // てこなし総括
        var sourceThrowOutControls = new List<ThrowOutControl>
        {
            new()
            {
                SourceId = route1Id,
                TargetId = route2Id,
                ControlType = ThrowOutControlType.WithoutLever
            }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1 },
            { route2Id, route2 },
            { tcId, tc }
        };

        var routeById = new Dictionary<ulong, Route> { { route1Id, route1 }, { route2Id, route2 } };

        // てこなし進路なので、leverは不要だがnullで渡す
        var result2 = CallCalculateLeverRelayState(
            route2,
            CreateRouteLeverDestinationButton(route2Id, 0, LR.Left), // ダミー
            null, // てこなし
            CreateButton("DummyP", RaiseDrop.Raise),
            null,
            sourceThrowOutControls,
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            new(),
            new(),
            new()
        );

        // Assert: Xリレーが扛上
        Assert.Equal(RaiseDrop.Raise, result2.IsXRelayRaised);
        // てこ反応リレーもXリレーと同じ
        Assert.Equal(RaiseDrop.Raise, result2.IsLeverRelayRaised);
    }

    [Fact(DisplayName = "てこなし総括_制御元のてこと制御先のボタンで進路が取れる")]
    public void RouteEstablished_WhenSourceLeverAndTargetButtonPressed_WithoutLeverThrowOut()
    {
        // Arrange
        var route1Id = 1ul;
        var lever1Id = 10ul;
        var route2Id = 2ul;
        var tcId = 100ul;

        var routeState1 = new RouteState
        {
            Id = route1Id,
            IsLeverRelayRaised = RaiseDrop.Raise,
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var routeState2 = new RouteState
        {
            Id = route2Id,
            IsLeverRelayRaised = RaiseDrop.Drop,
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop,
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route1 = CreateRoute(route1Id, routeState1);
        var lever1 = CreateLever(lever1Id, LCR.Left);

        var tc = CreateTrackCircuit(tcId, false);
        var route2 = CreateRoute(route2Id, routeState2, tcId);
        var button2 = CreateButton("P2", RaiseDrop.Raise); // ボタンを押す

        var sourceThrowOutControls = new List<ThrowOutControl>
        {
            new()
            {
                SourceId = route1Id,
                TargetId = route2Id,
                ControlType = ThrowOutControlType.WithoutLever
            }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1 },
            { route2Id, route2 },
            { tcId, tc }
        };

        var routeById = new Dictionary<ulong, Route> { { route1Id, route1 }, { route2Id, route2 } };

        var result2 = CallCalculateLeverRelayState(
            route2,
            CreateRouteLeverDestinationButton(route2Id, 0, LR.Left),
            null,
            button2,
            null,
            sourceThrowOutControls,
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            new(),
            new(),
            new()
        );

        // Assert: てこ反応リレーが扛上
        Assert.Equal(RaiseDrop.Raise, result2.IsLeverRelayRaised);
    }

    #endregion

    #region 6. 総括制御での信号制御後のてこ操作

    [Fact(DisplayName = "総括制御_制御元の信号制御落下後にてこを戻しても制御先のてこ反応リレーは扛上したまま")]
    public void TargetLeverRelayStaysRaised_WhenSourceLeverReturnedAfterSignalControlDropped()
    {
        // Arrange: route1が制御元、route2が制御先、総括制御で進路確保済み
        var route1Id = 1ul;
        var lever1Id = 10ul;
        var route2Id = 2ul;
        var lever2Id = 20ul;

        var routeState1 = new RouteState
        {
            Id = route1Id,
            IsLeverRelayRaised = RaiseDrop.Drop, // てこを戻したことにより、てこ反が落下した状態
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Drop,
            IsThrowOutYSRelayRaised = RaiseDrop.Drop,
            IsRouteLockRaised = RaiseDrop.Drop,
            IsSignalControlRaised = RaiseDrop.Drop, // 信号制御は落下している(軌道回路内方に進入した状態)
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var routeState2 = new RouteState
        {
            Id = route2Id,
            IsLeverRelayRaised = RaiseDrop.Raise, // 既に扛上している
            IsCtcRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRelayRaised = RaiseDrop.Drop,
            IsThrowOutXRRelayRaised = RaiseDrop.Raise, // XRリレー扛上
            IsThrowOutYSRelayRaised = RaiseDrop.Raise, // YSリレー扛上
            IsRouteLockRaised = RaiseDrop.Raise,
            IsSignalControlRaised = RaiseDrop.Raise, // 制御先の信号は現示している状態
            IsRouteRelayWithoutSwitchingMachineRaised = RaiseDrop.Drop
        };

        var route1 = CreateRoute(route1Id, routeState1);
        var lever1 = CreateLever(lever1Id, LCR.Center); // てこを戻した

        var route2 = CreateRoute(route2Id, routeState2);
        var lever2 = CreateLever(lever2Id, LCR.Center);
        var button2 = CreateButton("P2", RaiseDrop.Drop); // ボタンも離されている
        var routeLeverDestinationButton2 = CreateRouteLeverDestinationButton(route2Id, lever2Id, LR.Left, "P2");

        var sourceThrowOutControls = new List<ThrowOutControl>
        {
            new()
            {
                SourceId = route1Id,
                TargetId = route2Id,
                ControlType = ThrowOutControlType.WithLever
            }
        };

        var allSourceThrowOutControls = new Dictionary<ulong, List<ThrowOutControl>>
        {
            { route2Id, sourceThrowOutControls }
        };

        var interlockingObjectById = new Dictionary<ulong, InterlockingObject>
        {
            { route1Id, route1 },
            { lever1Id, lever1 },
            { route2Id, route2 },
            { lever2Id, lever2 }
        };

        var routeById = new Dictionary<ulong, Route> { { route1Id, route1 }, { route2Id, route2 } };
        var leverByRouteId = new Dictionary<ulong, Lever> { { route1Id, lever1 }, { route2Id, lever2 } };
        var routesByLeverId = new Dictionary<ulong, List<Route>>
        {
            { lever1Id, [route1] },
            { lever2Id, [route2] }
        };

        // Act
        var result2 = CallCalculateLeverRelayState(
            route2,
            routeLeverDestinationButton2,
            lever2,
            button2,
            null,
            sourceThrowOutControls,
            [],
            [],
            [],
            interlockingObjectById,
            routeById,
            leverByRouteId,
            routesByLeverId,
            allSourceThrowOutControls
        );

        // Assert: 制御先のてこ反応リレーは扛上したまま
        Assert.Equal(RaiseDrop.Raise, result2.IsLeverRelayRaised);
    }

    #endregion
}
