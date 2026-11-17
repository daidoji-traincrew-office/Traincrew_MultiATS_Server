using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteConflict;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class RouteConflictDbInitializerTest
{
    private readonly Mock<ILogger<RouteConflictDbInitializer>> _loggerMock = new();
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();
    private readonly Mock<ILockConditionRepository> _lockConditionRepositoryMock = new();
    private readonly Mock<ISwitchingMachineRouteRepository> _switchingMachineRouteRepositoryMock = new();
    private readonly Mock<IRouteConflictRepository> _routeConflictRepositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();

    private readonly List<RouteConflict> _added = [];
    private readonly List<RouteConflict> _deleted = [];

    private void Setup(
        List<ulong> routeIds,
        Dictionary<ulong, List<LockCondition>> lockConditions,
        List<SwitchingMachineRoute>? switchingMachineRoutes = null,
        List<RouteConflict>? existing = null)
    {
        _routeRepositoryMock.Setup(r => r.GetIdsForAll()).ReturnsAsync(routeIds);
        _lockConditionRepositoryMock
            .Setup(r => r.GetConditionsByObjectIdsAndType(It.IsAny<List<ulong>>(), LockType.Lock))
            .ReturnsAsync(lockConditions);
        _switchingMachineRouteRepositoryMock.Setup(r => r.GetAll())
            .ReturnsAsync(switchingMachineRoutes ?? []);
        _routeConflictRepositoryMock.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing ?? []);
        _generalRepositoryMock
            .Setup(r => r.AddAll(It.IsAny<IEnumerable<RouteConflict>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<RouteConflict>, CancellationToken>((e, _) => _added.AddRange(e))
            .Returns(Task.CompletedTask);
        _generalRepositoryMock
            .Setup(r => r.DeleteAll(It.IsAny<IEnumerable<RouteConflict>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<RouteConflict>, CancellationToken>((e, _) => _deleted.AddRange(e))
            .Returns(Task.CompletedTask);
    }

    private RouteConflictDbInitializer CreateInitializer() => new(
        _loggerMock.Object,
        _routeRepositoryMock.Object,
        _lockConditionRepositoryMock.Object,
        _switchingMachineRouteRepositoryMock.Object,
        _routeConflictRepositoryMock.Object,
        _generalRepositoryMock.Object);

    private static HashSet<(ulong, ulong)> Pairs(IEnumerable<RouteConflict> conflicts) =>
        conflicts.Select(c => (c.RouteId, c.ConflictingRouteId)).ToHashSet();

    [Fact]
    [DisplayName("鎖錠欄に進路がある場合、両方向の競合が登録されること")]
    public async Task ShouldAddBothDirections_WhenRouteIsInLockConditions()
    {
        Setup(
            [1, 2, 3],
            new() { { 1, [new LockConditionObject { ObjectId = 2 }] } });

        await CreateInitializer().InitializeRouteConflictAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new HashSet<(ulong, ulong)> { (1, 2), (2, 1) }, Pairs(_added));
        Assert.Empty(_deleted);
    }

    [Fact]
    [DisplayName("互いの鎖錠欄と転てつ器の両方で競合する場合でも、組は重複せず2件だけ登録されること")]
    public async Task ShouldNotDuplicate_WhenConflictIsFoundFromMultipleSources()
    {
        Setup(
            [1, 2],
            new()
            {
                { 1, [new LockConditionObject { ObjectId = 2 }] },
                { 2, [new LockConditionObject { ObjectId = 1 }] }
            },
            [
                new SwitchingMachineRoute { RouteId = 1, SwitchingMachineId = 10, IsReverse = NR.Normal },
                new SwitchingMachineRoute { RouteId = 2, SwitchingMachineId = 10, IsReverse = NR.Reversed }
            ]);

        await CreateInitializer().InitializeRouteConflictAsync(TestContext.Current.CancellationToken);

        // Pairs は HashSet なので重複を見逃す。件数も確かめる
        Assert.Equal(2, _added.Count);
        Assert.Equal(new HashSet<(ulong, ulong)> { (1, 2), (2, 1) }, Pairs(_added));
    }

    [Fact]
    [DisplayName("鎖錠欄の対象が進路以外の場合、競合は登録されないこと")]
    public async Task ShouldNotAdd_WhenLockTargetIsNotRoute()
    {
        Setup(
            [1, 2],
            new()
            {
                {
                    1, [
                        new LockConditionObject { ObjectId = 10, IsReverse = NR.Normal },
                        new LockConditionObject { ObjectId = 100 }
                    ]
                }
            });

        await CreateInitializer().InitializeRouteConflictAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_added);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<IEnumerable<RouteConflict>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [DisplayName("同じ転てつ器を定位と反位で鎖錠する進路同士は競合として登録されること")]
    public async Task ShouldAdd_WhenSwitchingMachinePositionIsOpposite()
    {
        Setup(
            [1, 2, 3],
            [],
            [
                new SwitchingMachineRoute { RouteId = 1, SwitchingMachineId = 10, IsReverse = NR.Normal },
                new SwitchingMachineRoute { RouteId = 2, SwitchingMachineId = 10, IsReverse = NR.Reversed },
                new SwitchingMachineRoute { RouteId = 3, SwitchingMachineId = 10, IsReverse = NR.Normal }
            ]);

        await CreateInitializer().InitializeRouteConflictAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new HashSet<(ulong, ulong)> { (1, 2), (2, 1), (3, 2), (2, 3) }, Pairs(_added));
    }

    [Fact]
    [DisplayName("同じ転てつ器を定位同士、反位同士で鎖錠する進路同士は競合として登録されないこと")]
    public async Task ShouldNotAdd_WhenSwitchingMachinePositionIsSame()
    {
        Setup(
            [1, 2, 3, 4],
            [],
            [
                new SwitchingMachineRoute { RouteId = 1, SwitchingMachineId = 10, IsReverse = NR.Normal },
                new SwitchingMachineRoute { RouteId = 2, SwitchingMachineId = 10, IsReverse = NR.Normal },
                new SwitchingMachineRoute { RouteId = 3, SwitchingMachineId = 20, IsReverse = NR.Reversed },
                new SwitchingMachineRoute { RouteId = 4, SwitchingMachineId = 20, IsReverse = NR.Reversed }
            ]);

        await CreateInitializer().InitializeRouteConflictAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_added);
        _generalRepositoryMock.Verify(
            r => r.AddAll(It.IsAny<IEnumerable<RouteConflict>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [DisplayName("登録済みの組は再登録せず、競合しなくなった組は削除されること")]
    public async Task ShouldSkipExistingAndDeleteStale()
    {
        Setup(
            [1, 2, 3],
            new() { { 1, [new LockConditionObject { ObjectId = 2 }] } },
            existing:
            [
                new RouteConflict { Id = 1, RouteId = 1, ConflictingRouteId = 2 },
                new RouteConflict { Id = 2, RouteId = 1, ConflictingRouteId = 3 }
            ]);

        await CreateInitializer().InitializeRouteConflictAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new HashSet<(ulong, ulong)> { (2, 1) }, Pairs(_added));
        Assert.Equal(new HashSet<(ulong, ulong)> { (1, 3) }, Pairs(_deleted));
    }
}
