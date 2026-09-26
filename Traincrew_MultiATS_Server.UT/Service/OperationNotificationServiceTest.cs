using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;
using Traincrew_MultiATS_Server.Services;
using Traincrew_MultiATS_Server.Services.Cache;
using Traincrew_MultiATS_Server.UT.TestHelpers;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// 運転告知器の状態キャッシュのテスト。
/// </summary>
/// <remarks>
/// 固定したいのは「司令卓の操作が即座に反映されること」と
/// 「500ms間隔のスケジューラが空振りのときに無効化しないこと」。
/// 後者を無条件に無効化するとキャッシュが意味を失う。
/// </remarks>
public class OperationNotificationServiceTest : ServiceTestBase, IDisposable
{
    private const string DisplayName = "TH75_1";
    private static readonly DateTime Now = new(2026, 9, 19, 3, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IOperationNotificationRepository> _repositoryMock = new();
    private readonly Mock<IGeneralRepository> _generalRepositoryMock = new();
    private readonly Mock<IDateTimeRepository> _dateTimeRepositoryMock = new();
    private readonly Mock<IOperationNotificationMasterStore> _masterStoreMock = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly InitializationState _initializationState = new();

    /// <summary>DB上の operation_notification_state を模した状態</summary>
    private readonly List<OperationNotificationState> _states =
    [
        new()
        {
            DisplayName = DisplayName,
            Type = OperationNotificationType.Tsuuchi,
            Content = "通知",
            OperatedAt = Now
        }
    ];

    /// <summary>スケジューラの更新が何行に当たるか</summary>
    private int _scheduledUpdateRowCount;

    public OperationNotificationServiceTest()
    {
        _initializationState.MarkInitialized();
        _dateTimeRepositoryMock.Setup(r => r.GetNow()).Returns(Now);
        _repositoryMock.Setup(r => r.GetAllStates())
            .ReturnsAsync(() => _states.ToList());
        _repositoryMock
            .Setup(r => r.SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(It.IsAny<DateTime>()))
            .ReturnsAsync(() => _scheduledUpdateRowCount);
        // 告知器マスタは「軌道回路1 → DisplayName」だけ持つ
        _masterStoreMock.Setup(s => s.Current)
            .Returns(new OperationNotificationMaster([
                new TrackCircuit
                {
                    Id = 1UL,
                    Name = "TC1",
                    Type = ObjectType.TrackCircuit,
                    OperationNotificationDisplayName = DisplayName
                }
            ]));
    }

    protected override void ConfigureTestServices(ServiceCollection services)
    {
        services.AddAllMocks();
        services.ReplaceMock(_repositoryMock);
        services.ReplaceMock(_generalRepositoryMock);
        services.ReplaceMock(_dateTimeRepositoryMock);
        services.ReplaceMock(_masterStoreMock);
        services.AddSingleton<ICacheGate>(
            _ => new CacheGate(_cache, new MutexRepository(), _initializationState));
        services.UseRealService<IOperationNotificationService, OperationNotificationService>();
    }

    public new void Dispose()
    {
        _cache.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    private void GivenDbContent(OperationNotificationType type, string content)
    {
        _states.Clear();
        _states.Add(new()
        {
            DisplayName = DisplayName,
            Type = type,
            Content = content,
            OperatedAt = Now
        });
    }

    [Fact(DisplayName = "ホットパスが2回目以降DBを読まないこと")]
    public async Task GetByTrackCircuitIds_UsesCacheOnSecondCall()
    {
        var service = GetService<IOperationNotificationService>();

        var first = await service.GetOperationNotificationDataByTrackCircuitIds([1UL]);
        var second = await service.GetOperationNotificationDataByTrackCircuitIds([1UL]);

        Assert.Equal("通知", first?.Content);
        Assert.Equal("通知", second?.Content);
        _repositoryMock.Verify(r => r.GetAllStates(), Times.Once);
    }

    [Fact(DisplayName = "告知器が引き当てられない在線ではDBを読まずnullを返すこと")]
    public async Task GetByTrackCircuitIds_WhenNoDisplay_ReturnsNullWithoutQuery()
    {
        var service = GetService<IOperationNotificationService>();

        Assert.Null(await service.GetOperationNotificationDataByTrackCircuitIds([999UL]));
        _repositoryMock.Verify(r => r.GetAllStates(), Times.Never);
    }

    [Fact(DisplayName = "司令卓の操作の直後からホットパスが新しい値を返すこと")]
    public async Task SetOperationNotificationData_InvalidatesCache()
    {
        var service = GetService<IOperationNotificationService>();
        // 先にキャッシュを充填しておく
        Assert.Equal("通知", (await service.GetOperationNotificationDataByTrackCircuitIds([1UL]))?.Content);

        // 司令卓が操作する(generalRepository.Save はモックなので、DB相当の状態は自分で進める)
        GivenDbContent(OperationNotificationType.Kaijo, "解除");
        await service.SetOperationNotificationData(new()
        {
            DisplayName = DisplayName,
            Type = OperationNotificationType.Kaijo,
            Content = "解除",
            OperatedAt = Now
        });

        var updated = await service.GetOperationNotificationDataByTrackCircuitIds([1UL]);
        Assert.Equal(OperationNotificationType.Kaijo, updated?.Type);
        Assert.Equal("解除", updated?.Content);
    }

    [Fact(DisplayName = "スケジューラが空振りのときはキャッシュを無効化しないこと")]
    public async Task SetNoneScheduler_WhenNoRowUpdated_KeepsCache()
    {
        // このスケジューラは500ms間隔で走る。無条件に無効化するとキャッシュが意味を失う
        var service = GetService<IOperationNotificationService>();
        await service.GetOperationNotificationDataByTrackCircuitIds([1UL]);
        _scheduledUpdateRowCount = 0;

        await service.SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime();
        await service.GetOperationNotificationDataByTrackCircuitIds([1UL]);

        // 充填の1回だけ
        _repositoryMock.Verify(r => r.GetAllStates(), Times.Once);
    }

    [Fact(DisplayName = "スケジューラが実際に行を戻したときはキャッシュを無効化すること")]
    public async Task SetNoneScheduler_WhenRowUpdated_InvalidatesCache()
    {
        var service = GetService<IOperationNotificationService>();
        GivenDbContent(OperationNotificationType.Kaijo, "解除");
        Assert.Equal(OperationNotificationType.Kaijo,
            (await service.GetOperationNotificationDataByTrackCircuitIds([1UL]))?.Type);

        // 20秒経過して「なし」に戻る
        _scheduledUpdateRowCount = 1;
        GivenDbContent(OperationNotificationType.None, string.Empty);
        await service.SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime();

        Assert.Equal(OperationNotificationType.None,
            (await service.GetOperationNotificationDataByTrackCircuitIds([1UL]))?.Type);
    }

    [Fact(DisplayName = "司令卓用の全件取得もキャッシュ経由であること")]
    public async Task GetOperationNotificationData_UsesSameCache()
    {
        var service = GetService<IOperationNotificationService>();

        await service.GetOperationNotificationDataByTrackCircuitIds([1UL]);
        var all = await service.GetOperationNotificationData();

        Assert.Equal([DisplayName], all.Select(data => data.DisplayName));
        _repositoryMock.Verify(r => r.GetAllStates(), Times.Once);
    }
}
