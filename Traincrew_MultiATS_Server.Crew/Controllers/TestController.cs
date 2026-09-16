using Microsoft.AspNetCore.Mvc;
using Traincrew_MultiATS_Server.Activity;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

[ApiController]
[Route("test")]
public class TestController(
    ITrainService trainService,
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// 計測・負荷試験専用のエンドポイント群。
    /// 認可を掛けていない(開発時は endpointBuilders に AllowAnonymous が一括付与される)ため、
    /// 本番環境では存在しないものとして扱い、DB を書き換えられないようにする。
    /// </summary>
    private bool IsEnabled => environment.IsDevelopment();

    [HttpGet("")]
    public IActionResult Test()
    {
        if (!IsEnabled)
        {
            return NotFound();
        }

        return Ok("Test endpoint is working.");
    }

    [HttpPost("createatsdata")]
    public async Task<IActionResult> TestCreateAtsData([FromBody] AtsToServerData data)
    {
        if (!IsEnabled)
        {
            return NotFound();
        }

        try
        {
            var result = await trainService.CreateAtsData(1001, data);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("perf-stats")]
    public IActionResult GetPerfStats()
    {
        if (!IsEnabled)
        {
            return NotFound();
        }

        var stats = SpanTimingCollector.GetStats()
            .OrderByDescending(kv => kv.Value.TotalMs)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return Ok(stats);
    }

    [HttpPost("perf-stats/reset")]
    public IActionResult ResetPerfStats()
    {
        if (!IsEnabled)
        {
            return NotFound();
        }

        SpanTimingCollector.Reset();
        return Ok(new { reset = true });
    }

    /// <summary>
    /// 実運用に近い負荷を再現するロードテスト。
    /// 各クライアントは 10 両編成で、一定間隔で軌道回路を進行し、可視信号機も切り替わる。
    /// </summary>
    /// <param name="clients">同時稼働列車数</param>
    /// <param name="seconds">実行秒数</param>
    /// <param name="cars">1 編成あたりの両数</param>
    /// <param name="rate">1 クライアントあたりの呼び出し回数/秒</param>
    /// <param name="moveIntervalMs">軌道回路を 1 つ進めるまでの滞在時間(ms)</param>
    [HttpPost("load")]
    public async Task<IActionResult> LoadTest(
        [FromQuery] int clients = 14,
        [FromQuery] int seconds = 30,
        [FromQuery] int cars = 10,
        [FromQuery] int rate = 10,
        [FromQuery] int moveIntervalMs = 3000)
    {
        if (!IsEnabled)
        {
            return NotFound();
        }

        Console.WriteLine(
            $"[Load Test] {clients} clients x {rate} calls/sec x {seconds} sec, {cars} cars/train, move every {moveIntervalMs} ms");

        // 軌道回路・信号機のマスタを DB から取得し、クライアントごとに重複しないよう割り当てる
        const int circuitsPerClient = 6;
        List<string> trackCircuitNames;
        List<string> signalNames;
        await using (var setupScope = scopeFactory.CreateAsyncScope())
        {
            var trackCircuitRepository =
                setupScope.ServiceProvider.GetRequiredService<ITrackCircuitRepository>();
            var nextSignalRepository =
                setupScope.ServiceProvider.GetRequiredService<INextSignalRepository>();
            trackCircuitNames = (await trackCircuitRepository.GetAllNames()).Order().ToList();
            signalNames = (await nextSignalRepository.GetAllAsync())
                .Select(ns => ns.SignalName)
                .Distinct()
                .Order()
                .ToList();
        }

        if (trackCircuitNames.Count < clients * circuitsPerClient)
        {
            return BadRequest(
                $"軌道回路が不足しています (必要: {clients * circuitsPerClient}, 実際: {trackCircuitNames.Count})");
        }

        if (signalNames.Count == 0)
        {
            // 0 除算になるので、マスタ未投入のまま呼ばれたら明示的に弾く
            return BadRequest("信号機のマスタデータが投入されていません");
        }

        if (moveIntervalMs <= 0)
        {
            // position の計算で 0 除算になるため、全リクエストが例外で潰れる前に弾く
            return BadRequest("moveIntervalMs には 1 以上を指定してください");
        }

        SpanTimingCollector.Reset();

        var tasks = new List<Task>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(seconds));
        var intervalMs = Math.Max(1, 1000 / Math.Max(1, rate));

        // 呼び出しが全部失敗していても「速くなった」ように見えてしまうので、成否を数えて結果に載せる
        var successCount = 0;
        var errorCount = 0;
        string? firstError = null;

        for (var i = 0; i < clients; i++)
        {
            var driverId = (ulong)(i + 1);
            var trainNumber = (1000 + (i + 1) * 2).ToString();
            // このクライアント専用の軌道回路列（他クライアントと重複しない）
            var myCircuits = trackCircuitNames
                .Skip(i * circuitsPerClient)
                .Take(circuitsPerClient)
                .ToList();
            // 信号機は共有されうるので、クライアントごとにオフセットしてばらけさせる
            var mySignals = Enumerable.Range(0, 4)
                .Select(k => signalNames[(i * 4 + k) % signalNames.Count])
                .ToList();

            var task = Task.Run(async () =>
            {
                var random = new Random(unchecked((int)driverId) * 7919);
                var startedAt = Environment.TickCount64;
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var elapsed = Environment.TickCount64 - startedAt;
                        // 一定時間ごとに 1 つ先の軌道回路へ進む（2 回路にまたがって在線）
                        var position = (int)(elapsed / moveIntervalMs);
                        var head = myCircuits[position % myCircuits.Count];
                        var tail = myCircuits[(position + 1) % myCircuits.Count];

                        // 走行中の車両状態は毎回変化する
                        var bcPress = (float)(random.NextDouble() * 400.0);
                        var ampare = (float)(random.NextDouble() * 300.0 - 50.0);
                        var doorClose = position % 2 == 0;
                        var carStates = Enumerable.Range(0, cars)
                            .Select(c => new CarState
                            {
                                CarModel = "E233系",
                                HasPantograph = c % 2 == 0,
                                HasDriverCab = c == 0 || c == cars - 1,
                                HasConductorCab = c == 0 || c == cars - 1,
                                HasMotor = c % 2 == 1,
                                DoorClose = doorClose,
                                BC_Press = bcPress,
                                Ampare = ampare
                            })
                            .ToList();

                        var clientData = new AtsToServerData
                        {
                            DiaName = trainNumber,
                            OnTrackList =
                            [
                                new() { Name = head },
                                new() { Name = tail }
                            ],
                            CarStates = carStates,
                            BougoState = false,
                            IsTherePreviousTrainIgnore = false,
                            IsMaybeWarpIgnore = true,
                            Speed = 60f,
                            Acceleration = 0f,
                            VisibleSignalNames =
                            [
                                mySignals[position % mySignals.Count],
                                mySignals[(position + 1) % mySignals.Count]
                            ]
                        };

                        // SignalR同様、クライアントごとに独立したスコープで実行
                        await using var scope = scopeFactory.CreateAsyncScope();
                        var service = scope.ServiceProvider.GetRequiredService<ITrainService>();
                        await service.CreateAtsData(driverId, clientData);
                        Interlocked.Increment(ref successCount);

                        await Task.Delay(
                            Math.Max(0, intervalMs + random.Next(-10, 10)),
                            cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (System.Exception ex)
                    {
                        Interlocked.Increment(ref errorCount);
                        Interlocked.CompareExchange(ref firstError, ex.Message, null);
                        Console.WriteLine($"[Load Test] Error for client {driverId}: {ex.Message}");
                        // 例外時はここで待たないとビジーループになり、CPU 計測そのものが汚れる
                        try
                        {
                            await Task.Delay(intervalMs, cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
                // 打ち切りは Task.WhenAll を落とさないよう、トークンを渡さず自前で判定する
            }, CancellationToken.None);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        Console.WriteLine(
            $"[Load Test] Load test completed (success: {successCount}, error: {errorCount})");

        return Ok(new { clients, seconds, cars, rate, moveIntervalMs, successCount, errorCount, firstError });
    }
}
