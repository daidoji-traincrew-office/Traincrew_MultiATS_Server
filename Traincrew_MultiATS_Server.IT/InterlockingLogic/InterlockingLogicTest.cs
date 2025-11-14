using Microsoft.AspNetCore.SignalR.Client;
using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.IT.TestUtilities;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.IT.InterlockingLogic;

[Collection("WebApplication")]
public class InterlockingLogicTest(WebApplicationFixture factory) : IAsyncLifetime
{
    private static readonly string? _targetStationIds = Environment.GetEnvironmentVariable("TEST_STATION_ID");

    private IInterlockingHubContract? _hub;
    private HubConnection? _connection;
    private TaskCompletionSource<DataToInterlocking>? _dataReceived;
    private DataToInterlocking? _latestData;

    public async ValueTask InitializeAsync()
    {
        // InterlockingHub接続を確立
        _dataReceived = new();

        var mockClientContract = new Mock<IInterlockingClientContract>();
        mockClientContract
            .Setup(client => client.ReceiveData(It.IsAny<DataToInterlocking>()))
            .Callback<DataToInterlocking>(data =>
            {
                _latestData = data;
                if (!_dataReceived.Task.IsCompleted)
                {
                    _dataReceived.TrySetResult(data);
                }
            })
            .Returns(Task.CompletedTask);

        (_connection, _hub) = factory.CreateInterlockingHub(mockClientContract.Object);
        await _connection.StartAsync(TestContext.Current.CancellationToken);

        // 初回データ受信を待機
        await _dataReceived.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }

    /// <summary>
    /// テストケース生成
    /// </summary>
    public static TheoryData<RouteTestCase> GetTestCases()
    {
        // WebApplicationFixtureのインスタンスを作成
        var fixture = new WebApplicationFixture();
        fixture.InitializeAsync().GetAwaiter().GetResult();

        try
        {
            // 環境変数TEST_STATION_IDで駅を指定可能
            // 単一駅: TEST_STATION_ID=TH76
            // 複数駅: TEST_STATION_ID=TH58,TH59,TH61 (カンマ区切り)
            // 未指定の場合は全駅のテストケースを生成
            string[]? stationIds = null;
            if (!string.IsNullOrEmpty(_targetStationIds))
            {
                stationIds = _targetStationIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            var generator = new RouteTestCaseGenerator(
                fixture.Create<IStationRepository>(),
                fixture.Create<IRouteLeverDestinationRepository>(),
                fixture.Create<IRouteRepository>(),
                fixture.Create<ISignalRouteRepository>(),
                fixture.Create<ILeverRepository>());
            var testCases = generator.GenerateTestCasesAsync(stationIds).GetAwaiter().GetResult();
            return new(testCases);
        }
        finally
        {
            fixture.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public async Task 進路構成テスト(RouteTestCase testCase)
    {
        if (_hub == null || _latestData == null)
        {
            throw new InvalidOperationException("InterlockingHubが初期化されていません");
        }

        try
        {
            var success = await ExecuteRouteTestAsync(testCase, _hub);
            Assert.True(success,
                $"進路 {testCase.RouteName} の信号機 {testCase.SignalName} が開通しませんでした");
        }
        finally
        {
            // クリーンアップ: てこを定位に戻す
            await _hub.SetPhysicalLeverData(new()
            {
                Name = testCase.LeverName,
                State = LCR.Center
            });
            // クリーンアップ後、少し待機して状態を安定させる
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// 進路テストの実行ロジック
    /// </summary>
    private async Task<bool> ExecuteRouteTestAsync(
        RouteTestCase testCase,
        IInterlockingHubContract hub)
    {
        // 1. てこ操作
        await hub.SetPhysicalLeverData(new()
        {
            Name = testCase.LeverName,
            State = testCase.LeverDirection
        });

        // 2. 着点操作（着点ボタンが存在する場合のみ）
        if (!string.IsNullOrEmpty(testCase.DestinationButtonName))
        {
            await hub.SetDestinationButtonState(new()
            {
                Name = testCase.DestinationButtonName,
                IsRaised = RaiseDrop.Raise,
                OperatedAt = DateTime.UtcNow
            });
        }

        // 3. 即時確認: 信号開通確認
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        var signal = _latestData?.Signals.FirstOrDefault(s => s.Name == testCase.SignalName);

        if (signal != null && signal.phase != Phase.None && signal.phase != Phase.R)
        {
            // 即時開通した場合は成功
            return true;
        }

        // 4. 7秒待機 → 再確認（転てつ器の転換完了を考慮）
        await Task.Delay(TimeSpan.FromSeconds(7), TestContext.Current.CancellationToken);

        signal = _latestData?.Signals.FirstOrDefault(s => s.Name == testCase.SignalName);

        // 7秒待機後に開通した場合は成功
        return signal != null && signal.phase != Phase.None && signal.phase != Phase.R;
    }
}
