using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
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
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

namespace Traincrew_MultiATS_Server.IT.InterlockingLogic;

[Collection("WebApplication")]
public class InterlockingLogicTest(WebApplicationFixture factory) : IAsyncLifetime
{
    private static readonly string? _targetStationIds = Environment.GetEnvironmentVariable("TEST_STATION_ID");

    private IInterlockingHubContract? _hub;
    private HubConnection? _connection;
    private TaskCompletionSource<DataToInterlocking>? _dataReceived;
    private volatile DataToInterlocking? _latestData;

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
        // MemberDataを利用する場合、このメソッドはStaticにしないといけないが
        // StaticメソッドだとFixtureが使えないので、やむなくここで定義している
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

            // スコープを明示的に管理してリソースリークを防止
            using var scope = fixture.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var generator = new RouteTestCaseGenerator(
                serviceProvider.GetRequiredService<IStationRepository>(),
                serviceProvider.GetRequiredService<IRouteLeverDestinationRepository>(),
                serviceProvider.GetRequiredService<IRouteRepository>(),
                serviceProvider.GetRequiredService<ISignalRouteRepository>(),
                serviceProvider.GetRequiredService<ILeverRepository>(),
                serviceProvider.GetRequiredService<IThrowOutControlRepository>());
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

        // 3. 次のデータ更新と、連動装置処理まで待つ
        await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);

        // 4. 1秒ポーリングで確認（転てつ器の転換完了を考慮、最大7秒まで）
        const int maxSeconds = 7;
        foreach (var i in Enumerable.Range(0, maxSeconds + 1))
        {
            // 開通した場合は成功
            if (CheckSignalsOpen(testCase))
            {
                return true;
            }

            if (i < maxSeconds)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            }
        }

        return false;
    }

    /// <summary>
    /// 信号が開通しているかチェック
    /// てこなし総括元の場合は、総括先の信号も確認する
    /// </summary>
    private bool CheckSignalsOpen(RouteTestCase testCase)
    {
        // ローカル変数にコピーして、途中で変更されないようにする（競合状態を防止）
        var currentData = _latestData;

        // メイン信号機の開通確認
        // currentDataがNullの場合、mainSignalはNullになる
        var mainSignal = currentData?.Signals.FirstOrDefault(s => s.Name == testCase.SignalName);
        if (mainSignal == null || mainSignal.phase == Phase.None || mainSignal.phase == Phase.R)
        {
            return false;
        }

        // てこなし総括先の信号機も確認（存在する場合）
        if (testCase.ThrowOutControlTargetSignals is not { Count: > 0 })
        {
            return true;
        }

        foreach (var targetSignalName in testCase.ThrowOutControlTargetSignals)
        {
            var targetSignal = currentData.Signals.FirstOrDefault(s => s.Name == targetSignalName);
            if (targetSignal == null || targetSignal.phase == Phase.None || targetSignal.phase == Phase.R)
            {
                return false;
            }
        }

        return true;
    }
}