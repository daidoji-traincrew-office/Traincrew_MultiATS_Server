using Microsoft.Extensions.Caching.Memory;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Protection;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// ProtectionService の IMemoryCache を使ったキャッシュ動作のテスト
/// </summary>
/// <remarks>
/// IMutexRepository は Mock ではなく実物の <see cref="MutexRepository"/> を使う。
/// Mock だと何も直列化されず、キャッシュ充填と無効化の競合テストが素通りしてしまうため。
/// </remarks>
public class ProtectionServiceCacheTest
{
    private static (
        ProtectionService service,
        Mock<IProtectionRepository> protectionRepoMock,
        Mock<IGeneralRepository> generalRepoMock,
        IMemoryCache cache) CreateService()
    {
        var protectionRepoMock = new Mock<IProtectionRepository>();
        var generalRepoMock = new Mock<IGeneralRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        // 実物のミューテックス(Crew/Passenger では Singleton 登録)
        var mutexRepository = new MutexRepository();
        var service = new ProtectionService(
            protectionRepoMock.Object, generalRepoMock.Object, mutexRepository, cache);
        return (service, protectionRepoMock, generalRepoMock, cache);
    }

    private static TrackCircuit TrackCircuit(int protectionZone)
    {
        return new TrackCircuit { Name = $"TC{protectionZone}", ProtectionZone = protectionZone };
    }

    private static ProtectionZoneState ZoneState(ulong id, string trainNumber, int protectionZone)
    {
        return new ProtectionZoneState { id = id, TrainNumber = trainNumber, ProtectionZone = protectionZone };
    }

    [Fact]
    public async Task GetProtectionRadioStates_CalledTwice_RepositoryCalledOnce()
    {
        var (service, protectionRepoMock, _, _) = CreateService();
        protectionRepoMock
            .Setup(r => r.GetProtectionZoneStates())
            .ReturnsAsync(() => [ZoneState(1, "1234A", 10)]);

        var first = await service.GetProtectionRadioStates();
        var second = await service.GetProtectionRadioStates();

        Assert.Single(first);
        Assert.Single(second);
        protectionRepoMock.Verify(r => r.GetProtectionZoneStates(), Times.Once);
    }

    [Fact]
    public async Task IsProtectionEnabledForTrackCircuits_CalledTwice_RepositoryCalledOnce()
    {
        var (service, protectionRepoMock, _, _) = CreateService();
        protectionRepoMock
            .Setup(r => r.GetProtectionZoneStates())
            .ReturnsAsync(() => [ZoneState(1, "1234A", 10)]);

        var trackCircuits = new List<TrackCircuit> { TrackCircuit(10) };
        Assert.True(await service.IsProtectionEnabledForTrackCircuits(trackCircuits));
        Assert.True(await service.IsProtectionEnabledForTrackCircuits(trackCircuits));

        protectionRepoMock.Verify(r => r.GetProtectionZoneStates(), Times.Once);
    }

    [Fact]
    public async Task AddProtectionZoneState_InvalidatesCache_LatestValueReturned()
    {
        var (service, protectionRepoMock, generalRepoMock, _) = CreateService();
        var store = new List<ProtectionZoneState>();
        protectionRepoMock
            .Setup(r => r.GetProtectionZoneStates())
            .ReturnsAsync(() => store.ToList());
        generalRepoMock
            .Setup(r => r.Add(It.IsAny<ProtectionZoneState>()))
            .Callback<ProtectionZoneState>(e => store.Add(e))
            .Returns(Task.CompletedTask);

        Assert.Empty(await service.GetProtectionRadioStates());

        await service.AddProtectionZoneState(new ProtectionRadioData { TrainNumber = "1234A", ProtectionZone = 10 });

        var after = await service.GetProtectionRadioStates();
        Assert.Single(after);
        Assert.Equal("1234A", after[0].TrainNumber);
        protectionRepoMock.Verify(r => r.GetProtectionZoneStates(), Times.Exactly(2));
    }

    /// <summary>
    /// 競合の回帰テスト。
    /// キャッシュ充填の SELECT が実行されている最中に無効化(指令卓からの発報)が走っても、
    /// SELECT 完了後に陳腐化した値がキャッシュに残らないことを検証する。
    /// GetOrCreateAsync を使った修正前の実装では、ファクトリ実行中の Remove は何も消さず
    /// 陳腐化した値がそのまま格納されるため、このテストは落ちる。
    /// </summary>
    [Fact]
    public async Task SlowSelect_ConcurrentInvalidation_StaleValueIsNotCached()
    {
        var (service, protectionRepoMock, generalRepoMock, _) = CreateService();

        var store = new List<ProtectionZoneState>();
        var selectStarted = new TaskCompletionSource();
        var releaseSelect = new TaskCompletionSource();
        var addCalled = new TaskCompletionSource();
        var isFirstSelect = true;

        protectionRepoMock
            .Setup(r => r.GetProtectionZoneStates())
            .Returns(async () =>
            {
                if (isFirstSelect)
                {
                    isFirstSelect = false;
                    // SELECT 開始時点のスナップショット(= この後の書き込みは見えない)
                    var snapshot = store.ToList();
                    selectStarted.TrySetResult();
                    // 「遅い SELECT」。この間に無効化が走る
                    await releaseSelect.Task;
                    return snapshot;
                }

                return store.ToList();
            });
        generalRepoMock
            .Setup(r => r.Add(It.IsAny<ProtectionZoneState>()))
            .Callback<ProtectionZoneState>(e =>
            {
                store.Add(e);
                addCalled.TrySetResult();
            })
            .Returns(Task.CompletedTask);

        // 1. 遅い SELECT を開始させる(この時点の DB は空)
        var readTask = service.GetProtectionRadioStates();
        await selectStarted.Task;

        // 2. その最中に指令卓由来の発報(DB 書き込み + キャッシュ無効化)を走らせる
        var addTask = service.AddProtectionZoneState(
            new ProtectionRadioData { TrainNumber = "1234A", ProtectionZone = 10 });
        await addCalled.Task;
        // 無効化がキャッシュ充填と衝突するタイミングまで進めるための猶予
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // 3. SELECT を完了させる(空のリストが返る = 陳腐化した値)
        releaseSelect.TrySetResult();
        var duringWrite = await readTask;
        await addTask;

        // 進行中の読み取りが古い値を返すこと自体は許容する
        Assert.Empty(duringWrite);

        // 陳腐化した値がキャッシュに残っていないこと(残っていると TTL の 10 秒間発報が見えない)
        var after = await service.GetProtectionRadioStates();
        Assert.Single(after);
        Assert.Equal("1234A", after[0].TrainNumber);
    }

    [Fact]
    public async Task UpdateBougoState_False_WithExistingRow_IssuesDelete()
    {
        var (service, protectionRepoMock, _, _) = CreateService();
        protectionRepoMock
            .Setup(r => r.GetProtectionZoneStates())
            .ReturnsAsync(() => [ZoneState(1, "1234A", 10)]);

        await service.UpdateBougoState("1234A", [TrackCircuit(10)], false);

        protectionRepoMock.Verify(r => r.DisableProtection("1234A"), Times.Once);
    }

    [Fact]
    public async Task UpdateBougoState_False_AfterCommanderAdded_IssuesDelete()
    {
        var (service, protectionRepoMock, generalRepoMock, _) = CreateService();
        var store = new List<ProtectionZoneState>();
        protectionRepoMock
            .Setup(r => r.GetProtectionZoneStates())
            .ReturnsAsync(() => store.ToList());
        generalRepoMock
            .Setup(r => r.Add(It.IsAny<ProtectionZoneState>()))
            .Callback<ProtectionZoneState>(e => store.Add(e))
            .Returns(Task.CompletedTask);
        protectionRepoMock
            .Setup(r => r.DisableProtection(It.IsAny<string>()))
            .Callback<string>(trainNumber => store.RemoveAll(s => s.TrainNumber == trainNumber))
            .Returns(Task.CompletedTask);

        // 発報していない状態をキャッシュに載せる
        await service.UpdateBougoState("1234A", [TrackCircuit(10)], false);
        protectionRepoMock.Verify(r => r.DisableProtection(It.IsAny<string>()), Times.Never);

        // 指令卓から発報される
        await service.AddProtectionZoneState(new ProtectionRadioData { TrainNumber = "1234A", ProtectionZone = 10 });

        // 陳腐化キャッシュによる早期 return が起きず、DELETE が発行されること
        await service.UpdateBougoState("1234A", [TrackCircuit(10)], false);
        protectionRepoMock.Verify(r => r.DisableProtection("1234A"), Times.Once);
        Assert.Empty(store);
    }

    [Fact]
    public async Task UpdateBougoState_True_AfterEnable_SkipsRedundantDbWrite()
    {
        var (service, protectionRepoMock, _, _) = CreateService();
        var store = new List<ProtectionZoneState>();
        protectionRepoMock
            .Setup(r => r.GetProtectionZoneStates())
            .ReturnsAsync(() => store.ToList());
        protectionRepoMock
            .Setup(r => r.EnableProtection(It.IsAny<string>(), It.IsAny<List<int>>()))
            .Callback<string, List<int>>((trainNumber, zones) =>
            {
                store.RemoveAll(s => s.TrainNumber == trainNumber);
                store.AddRange(zones.Select((z, i) => ZoneState((ulong)i + 1, trainNumber, z)));
            })
            .Returns(Task.CompletedTask);

        await service.UpdateBougoState("1234A", [TrackCircuit(10)], true);
        await service.UpdateBougoState("1234A", [TrackCircuit(10)], true);

        protectionRepoMock.Verify(
            r => r.EnableProtection(It.IsAny<string>(), It.IsAny<List<int>>()), Times.Once);
    }
}
