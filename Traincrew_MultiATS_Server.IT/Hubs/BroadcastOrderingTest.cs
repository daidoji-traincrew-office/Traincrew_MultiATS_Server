using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.IT.Hubs;

/// <summary>
/// BroadcastScheduler が連動盤へ配信する順序を検証する。
///
/// 統合前は InterlockingHubScheduler(ReceiveData) と SignalScheduler(ReceiveSignalData) が
/// 位相の揃わない独立ループで別々のDBスナップショットを読んでいたため、
/// 「信号ランプがラインライトより先に変わる」という因果順の逆転が確率的に起きていた。
/// 統合後は1ティック=1スナップショットかつ逐次送信になるため決定的に順序が保たれる。
/// </summary>
[Collection("WebApplication")]
public class BroadcastOrderingTest(WebApplicationFixture factory)
{
    /// <summary>対象軌道回路。signal.track_circuit_id で下記信号機と結びついている</summary>
    private const string TargetTrackCircuit = "上り102T";

    /// <summary>対象信号機。自分の軌道回路が短絡すると停止現示(R)になる</summary>
    private const string TargetSignal = "上り閉塞102";

    /// <summary>逆転はタイミング依存なので複数回繰り返して全回パスすることを条件にする</summary>
    private const int IterationCount = 15;

    private sealed record Frame(int Seq, string Kind, bool? TrackCircuitOn, Phase? SignalPhase);

    [Fact]
    public async Task 連動盤への配信でラインライトが信号現示より先に届くこと()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seq = 0;
        var observedTransitionCount = 0;
        var totalFrameCount = 0;
        var frames = new List<Frame>();
        var framesLock = new object();

        var mockClientContract = new Mock<IInterlockingClientContract>();
        mockClientContract
            .Setup(client => client.ReceiveData(It.IsAny<DataToInterlocking>()))
            .Callback<DataToInterlocking>(data =>
            {
                var on = data.TrackCircuits
                    .FirstOrDefault(trackCircuit => trackCircuit.Name == TargetTrackCircuit)?.On ?? false;
                lock (framesLock)
                {
                    frames.Add(new Frame(Interlocked.Increment(ref seq), "Data", on, null));
                }
            })
            .Returns(Task.CompletedTask);
        mockClientContract
            .Setup(client => client.ReceiveSignalData(It.IsAny<List<SignalData>>()))
            .Callback<List<SignalData>>(signalData =>
            {
                var phase = signalData
                    .FirstOrDefault(signal => signal.Name == TargetSignal)?.phase;
                lock (framesLock)
                {
                    frames.Add(new Frame(Interlocked.Increment(ref seq), "Signal", null, phase));
                }
            })
            .Returns(Task.CompletedTask);

        // 公開側ITには連動サーバーが居ないため、放置するとSignalServiceの
        // フェイルセーフ判定(IsInterlockingHealthy)で全信号が常時R現示になり
        // 「R以外→R」の変化を観測できない。連動サーバーのハートビートを肩代わりする。
        using var heartbeatCts = new CancellationTokenSource();
        var heartbeatTask = SimulateInterlockingHeartbeatAsync(heartbeatCts.Token);

        try
        {
            var (connection, _) = factory.CreateInterlockingHub(mockClientContract.Object);
            await using (connection)
            {
                await connection.StartAsync(cancellationToken);

                for (var iteration = 0; iteration < IterationCount; iteration++)
                {
                    // 進入前の状態に戻し、対象信号がR以外になるまで待つ(比較の基準を作る)
                    await SetTrackCircuitOnAsync(false);
                    await Task.Delay(TimeSpan.FromMilliseconds(900), cancellationToken);

                    lock (framesLock)
                    {
                        frames.Clear();
                    }

                    // 在線させる
                    await SetTrackCircuitOnAsync(true);
                    await Task.Delay(TimeSpan.FromMilliseconds(900), cancellationToken);

                    List<Frame> observed;
                    lock (framesLock)
                    {
                        observed = frames.ToList();
                    }

                    if (AssertCausalOrder(observed, iteration))
                    {
                        observedTransitionCount++;
                    }

                    AssertAlternating(observed, iteration);
                    totalFrameCount += observed.Count;
                }
            }

            // このテストが空振り(条件を一度も観測できず全スキップ)していないことを保証する。
            // 例えば連動サーバーのハートビート肩代わりが効かず全信号が常時R現示になると、
            // 「R以外→R」の変化が観測できずアサーションが素通りしてしまう。
            Assert.True(totalFrameCount > 0, "連動盤への配信を一度も受信していない");
            Assert.True(
                observedTransitionCount > 0,
                "軌道回路の在線と信号の停止現示への変化を一度も観測できていない。"
                + "テストが空振りしているため、順序の検証になっていない");
        }
        finally
        {
            await heartbeatCts.CancelAsync();
            await heartbeatTask;
            await SetTrackCircuitOnAsync(false);
            await RestoreDegradedStateAsync();
        }
    }

    /// <summary>
    /// アサーション1(本命): 軌道回路の落下が信号の停止現示より先に(または同時に)届くこと。
    /// </summary>
    private static bool AssertCausalOrder(List<Frame> observed, int iteration)
    {
        var firstTrackCircuitOn = observed
            .FirstOrDefault(frame => frame is { Kind: "Data", TrackCircuitOn: true });
        var firstSignalStop = observed
            .FirstOrDefault(frame => frame is { Kind: "Signal", SignalPhase: Phase.R });

        // 変化を観測できた場合のみ比較する(周期の都合でどちらかを取り逃す場合がある)
        if (firstTrackCircuitOn is null || firstSignalStop is null)
        {
            return false;
        }

        Assert.True(
            firstTrackCircuitOn.Seq <= firstSignalStop.Seq,
            $"iteration {iteration}: 信号現示がラインライトより先に届いた "
            + $"(TrackCircuit seq={firstTrackCircuitOn.Seq}, Signal seq={firstSignalStop.Seq})");

        return true;
    }

    /// <summary>
    /// アサーション2: BroadcastScheduler だけが連動盤へ送っているなら
    /// Data, Signal, Data, Signal, ... と交互になる。
    /// 崩れた場合は旧SignalSchedulerの削除漏れ、または二重配信を意味する。
    /// </summary>
    private static void AssertAlternating(List<Frame> observed, int iteration)
    {
        for (var i = 1; i < observed.Count; i++)
        {
            Assert.True(
                observed[i - 1].Kind != observed[i].Kind,
                $"iteration {iteration}: 配信が交互になっていない "
                + $"({observed[i - 1].Kind} -> {observed[i].Kind} at seq={observed[i].Seq})。"
                + "旧スケジューラの削除漏れ、または二重配信の可能性がある");
        }
    }

    private async Task SetTrackCircuitOnAsync(bool on)
    {
        using var scope = factory.Services.CreateScope();
        var trackCircuitService = scope.ServiceProvider.GetRequiredService<ITrackCircuitService>();
        await trackCircuitService.SetTrackCircuitData(new TrackCircuitData
        {
            Name = TargetTrackCircuit,
            Last = on ? "回1" : "",
            On = on,
            Lock = false
        });
    }

    private async Task SimulateInterlockingHeartbeatAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using (var scope = factory.Services.CreateScope())
            {
                var serverRepository = scope.ServiceProvider.GetRequiredService<IServerRepository>();
                await serverRepository.SetIsAllSignalRelayRaisedAsync(RaiseDropWithForce.Raise);
                await serverRepository.SetInterlockingHeartbeatAtAsync(DateTime.Now);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 他のテストに影響しないよう、連動サーバー停止相当の状態へ戻す。
    /// </summary>
    private async Task RestoreDegradedStateAsync()
    {
        using var scope = factory.Services.CreateScope();
        var serverRepository = scope.ServiceProvider.GetRequiredService<IServerRepository>();
        await serverRepository.SetIsAllSignalRelayRaisedAsync(RaiseDropWithForce.Drop);
    }
}
