using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Traincrew_MultiATS_Server.Hubs;

namespace Traincrew_MultiATS_Server.UT.Hubs;

/// <summary>
/// latest-only配信(接続ごとのメールボックス)のテスト。
/// </summary>
/// <remarks>
/// HubConnectionContext は DefaultConnectionContext + Pipe で実物を作る。
/// 「詰まった接続」は PauseWriterThreshold を1バイトにしたPipeを読まないことで再現する。
/// </remarks>
public class LatestOnlyHubLifetimeManagerTest
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private class TestHub : Hub;

    private sealed class DuplexPipe(PipeReader input, PipeWriter output) : IDuplexPipe
    {
        public PipeReader Input { get; } = input;
        public PipeWriter Output { get; } = output;
    }

    /// <summary>
    /// 接続1本分。Server側(HubConnectionContext)が書いたものを Client 側(Reader)で読む。
    /// </summary>
    private sealed class TestConnection : IAsyncDisposable
    {
        private readonly Pipe _toServer = new();
        private readonly Pipe _toClient;
        private readonly List<byte> _buffer = [];

        public HubConnectionContext Context { get; }

        public TestConnection(string connectionId, bool blocked)
        {
            // blocked: 1バイトでも溜まるとFlushが待たされる(=クライアントが読まない接続の再現)
            _toClient = blocked
                ? new Pipe(new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: 1))
                : new Pipe();
            var transport = new DuplexPipe(_toServer.Reader, _toClient.Writer);
            var application = new DuplexPipe(_toClient.Reader, _toServer.Writer);
            var connectionContext = new DefaultConnectionContext(connectionId, transport, application);
            Context = new HubConnectionContext(connectionContext, new HubConnectionContextOptions(),
                NullLoggerFactory.Instance)
            {
                Protocol = new JsonHubProtocol()
            };
        }

        /// <summary>
        /// 条件を満たすまで(または上限時間まで)受信メッセージを読む。
        /// </summary>
        public async Task<List<(string Method, int Value)>> ReadUntilAsync(
            Func<IReadOnlyList<(string Method, int Value)>, bool> completed)
        {
            var result = new List<(string, int)>();
            using var cts = new CancellationTokenSource(Timeout);
            while (!completed(result))
            {
                var readResult = await _toClient.Reader.ReadAsync(cts.Token);
                var buffer = readResult.Buffer;
                _buffer.AddRange(buffer.ToArray());
                _toClient.Reader.AdvanceTo(buffer.End);

                // メッセージは0x1e(レコードセパレータ)区切り
                int separatorIndex;
                while ((separatorIndex = _buffer.IndexOf(0x1e)) >= 0)
                {
                    var json = _buffer.GetRange(0, separatorIndex).ToArray();
                    _buffer.RemoveRange(0, separatorIndex + 1);
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    result.Add((root.GetProperty("target").GetString()!,
                        root.GetProperty("arguments")[0].GetInt32()));
                }
            }

            return result;
        }

        /// <summary>
        /// クライアント側にデータが届くまで(または上限時間まで)待つ。データは消費しない。
        /// 書き手(pump)のFlushは再開されないまま=詰まった状態が保たれ、後でReadUntilAsyncを呼べば同じデータを読める。
        /// </summary>
        public async Task WaitForUnconsumedDataAsync()
        {
            using var cts = new CancellationTokenSource(Timeout);
            var readResult = await _toClient.Reader.ReadAsync(cts.Token);
            var buffer = readResult.Buffer;
            // consumed=Start / examined=Start なので、未消費データは残り次の ReadAsync は即座に返す
            _toClient.Reader.AdvanceTo(buffer.Start, buffer.Start);
        }

        public bool HasPendingData() => _toClient.Reader.TryRead(out var r) && r.Buffer.Length > 0;

        public async ValueTask DisposeAsync()
        {
            await _toClient.Reader.CompleteAsync();
            await _toClient.Writer.CompleteAsync();
            await _toServer.Reader.CompleteAsync();
            await _toServer.Writer.CompleteAsync();
        }
    }

    private static LatestOnlyHubLifetimeManager<TestHub> CreateManager()
    {
        return new LatestOnlyHubLifetimeManager<TestHub>(
            NullLogger<LatestOnlyHubLifetimeManager<TestHub>>.Instance,
            NullLogger<DefaultHubLifetimeManager<TestHub>>.Instance);
    }

    [Fact]
    public async Task SendAllLatestAsync_詰まった接続があっても即完了し_他の接続は届き続ける()
    {
        var manager = CreateManager();
        await using var blocked = new TestConnection("blocked", true);
        await using var healthy = new TestConnection("healthy", false);
        await manager.OnConnectedAsync(blocked.Context);
        await manager.OnConnectedAsync(healthy.Context);

        // 健全な接続は並行して読み続ける
        var healthyReading = healthy.ReadUntilAsync(received => received.Any(x => x.Value == 99));

        for (var i = 0; i < 100; i++)
        {
            var task = manager.SendAllLatestAsync("Receive", [i]);
            Assert.True(task.IsCompleted);
        }

        var healthyReceived = await healthyReading;
        Assert.Equal(99, healthyReceived[^1].Value);
        // 健全な接続は値が単調増加で受け取る(古いデータが後から来ない)
        Assert.Equal(healthyReceived.Select(x => x.Value).Order(), healthyReceived.Select(x => x.Value));

        await manager.OnDisconnectedAsync(blocked.Context);
        await manager.OnDisconnectedAsync(healthy.Context);
    }

    [Fact]
    public async Task SendAllLatestAsync_詰まっていた接続は復帰時に最新だけを受け取る()
    {
        var manager = CreateManager();
        await using var blocked = new TestConnection("blocked", true);
        await manager.OnConnectedAsync(blocked.Context);

        for (var i = 0; i < 100; i++)
        {
            await manager.SendAllLatestAsync("Receive", [i]);
        }

        // 読み始める。送信中だった1件と最新の1件だけが届き、途中の値は捨てられている
        var received = await blocked.ReadUntilAsync(r => r.Any(x => x.Value == 99));

        Assert.Equal(99, received[^1].Value);
        Assert.True(received.Count <= 2, $"想定より多く届いた: {string.Join(",", received.Select(x => x.Value))}");

        await manager.OnDisconnectedAsync(blocked.Context);
    }

    [Fact]
    public async Task SendAllLatestAsync_異なるメソッドごとに最新1件が届く()
    {
        var manager = CreateManager();
        await using var blocked = new TestConnection("blocked", true);
        await manager.OnConnectedAsync(blocked.Context);

        for (var i = 0; i < 50; i++)
        {
            await manager.SendAllLatestAsync("A", [i]);
            await manager.SendAllLatestAsync("B", [1000 + i]);
        }

        var received = await blocked.ReadUntilAsync(r =>
            r.Any(x => x is { Method: "A", Value: 49 }) && r.Any(x => x is { Method: "B", Value: 1049 }));

        // メソッドごとに(送信中の1件+最新)以下
        Assert.True(received.Count(x => x.Method == "A") <= 2);
        Assert.True(received.Count(x => x.Method == "B") <= 2);

        await manager.OnDisconnectedAsync(blocked.Context);
    }

    [Fact]
    public async Task Mailbox_同じメソッドは上書きされ_順序位置はPost順を保つ()
    {
        await using var connection = new TestConnection("c", false);
        var mailbox = new ConnectionMailbox("TestHub", NullLogger.Instance);

        // pumpを開始する前に全部Postしておく(タイミングに依存させない)
        mailbox.Post("A", new SerializedHubMessage(new InvocationMessage("A", [1])));
        mailbox.Post("B", new SerializedHubMessage(new InvocationMessage("B", [2])));
        mailbox.Post("A", new SerializedHubMessage(new InvocationMessage("A", [3])));
        mailbox.Start(connection.Context);

        var received = await connection.ReadUntilAsync(r => r.Count >= 2);
        await mailbox.StopAsync();

        // Aは最新の3に上書きされ、位置はBより前のまま
        Assert.Equal([("A", 3), ("B", 2)], received);
    }

    private sealed class CapturingLogger : ILogger
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception,
            Func<TState, System.Exception?, string> formatter)
            => Entries.Enqueue((logLevel, formatter(state, exception)));
    }

    private sealed class TypedCapturingLogger<T>(CapturingLogger inner) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception,
            Func<TState, System.Exception?, string> formatter)
            => inner.Log(logLevel, eventId, state, exception, formatter);
    }

    private static SerializedHubMessage Message(string method, int value)
        => new(new InvocationMessage(method, [value]));

    // pumpの終了確認用
    private static Task GetPumpTask(ConnectionMailbox mailbox)
        => mailbox.PumpTask;

    [Fact]
    public async Task OnDisconnectedAsync_切断後はpumpが終了し_送信しても例外にならない()
    {
        var manager = CreateManager();
        await using var connection = new TestConnection("c", false);
        await manager.OnConnectedAsync(connection.Context);

        await manager.SendAllLatestAsync("Receive", [1]);
        var first = await connection.ReadUntilAsync(r => r.Count >= 1);
        Assert.Equal([("Receive", 1)], first);

        await manager.OnDisconnectedAsync(connection.Context).WaitAsync(Timeout);

        // 切断後のmailboxは辞書から外れているので、マネージャー経由では誰にも届かず例外にもならない
        await manager.SendAllLatestAsync("Receive", [2]);
    }

    [Fact]
    public async Task OnDisconnectedAsync_書き込みが詰まった接続でもタイムアウト内に完了する()
    {
        var manager = CreateManager();
        await using var blocked = new TestConnection("blocked", true);
        await manager.OnConnectedAsync(blocked.Context);

        // 1件目がpumpに書き込まれ、クライアントが読まないためFlushで待たされている状態を作る
        await manager.SendAllLatestAsync("Receive", [0]);
        await blocked.WaitForUnconsumedDataAsync();
        // 2件目以降はmailboxに溜まるだけ(pumpはFlushで詰まったまま)
        for (var i = 1; i < 3; i++)
        {
            await manager.SendAllLatestAsync("Receive", [i]);
        }

        // マネージャー経由の切断が、詰まった書き込みでハングしない
        await manager.OnDisconnectedAsync(blocked.Context).WaitAsync(Timeout);
    }

    [Fact]
    public async Task Mailbox_StopAsync後にPostしても例外にならず何も書かれない()
    {
        await using var connection = new TestConnection("c", false);
        var mailbox = new ConnectionMailbox("TestHub", NullLogger.Instance);
        mailbox.Start(connection.Context);
        mailbox.Post("Receive", Message("Receive", 1));
        await connection.ReadUntilAsync(r => r.Count >= 1);

        await mailbox.StopAsync().WaitAsync(Timeout);

        // 停止後に(スケジューラが辞書から外れる前のmailboxへ)Postが来ても、Disposeされた_ctsやCompleteしたChannelで落ちない
        mailbox.Post("Receive", Message("Receive", 2));
        await Task.Delay(300);

        Assert.False(connection.HasPendingData());
    }

    [Fact]
    public async Task OnDisconnectedAsync_送信を回し続けていてもタイムアウト内に完了する()
    {
        var manager = CreateManager();
        await using var connection = new TestConnection("c", false);
        await manager.OnConnectedAsync(connection.Context);

        using var stop = new CancellationTokenSource();
        var sender = Task.Run(async () =>
        {
            for (var i = 0; !stop.IsCancellationRequested; i++)
            {
                await manager.SendAllLatestAsync("Receive", [i]);
                await Task.Yield();
            }
        });
        // 送信が走り始めてから切断する
        await Task.Delay(100);

        try
        {
            await manager.OnDisconnectedAsync(connection.Context).WaitAsync(Timeout);
            // 送信側も例外を出さずに回り続けられている
            Assert.False(sender.IsFaulted);
        }
        finally
        {
            await stop.CancelAsync();
            await sender;
        }
    }

    [Fact]
    public async Task Mailbox_接続がAbortされるとStopAsyncなしでもpumpが終了する()
    {
        await using var connection = new TestConnection("c", false);
        var mailbox = new ConnectionMailbox("TestHub", NullLogger.Instance);
        mailbox.Start(connection.Context);
        mailbox.Post("Receive", Message("Receive", 1));
        await connection.ReadUntilAsync(r => r.Count >= 1);

        connection.Context.Abort();

        // StopAsyncは呼ばない。ConnectionAbortedだけでpumpが抜ける
        await GetPumpTask(mailbox).WaitAsync(Timeout);
        await mailbox.StopAsync().WaitAsync(Timeout);
    }

    [Fact]
    public async Task Mailbox_1件目の送信が詰まっている間に2件目を上書きすると_2件目は新しい値だけが届く()
    {
        var logger = new CapturingLogger();
        var mailbox = new ConnectionMailbox("TestHub", logger, TimeSpan.FromMilliseconds(100));
        await using var blocked = new TestConnection("blocked", true);

        // pump開始前にA→Bの順で置く。一括で取り出す実装だと、Bは取り出し時点の値(1)で固定される
        mailbox.Post("A", Message("A", 1));
        mailbox.Post("B", Message("B", 1));
        mailbox.Start(blocked.Context);

        // Aの送信が詰まっている(閾値超過のWarning)ことを確認してからBを上書きする
        var deadline = DateTime.UtcNow + Timeout;
        while (!logger.Entries.Any(e => e.Level == LogLevel.Warning) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
        mailbox.Post("B", Message("B", 2));

        // 詰まりを解除する。Bは古い値(1)を送らず、新しい値(2)だけが届く
        var received = await blocked.ReadUntilAsync(r => r.Any(x => x.Method == "B"));
        await mailbox.StopAsync().WaitAsync(Timeout);

        Assert.Equal([("A", 1), ("B", 2)], received);
    }

    [Fact]
    public async Task OnConnectedAsync_ConnectionIdが重複して登録できなくてもErrorが出て_先に登録した接続のmailboxは残る()
    {
        var logger = new CapturingLogger();
        var manager = new LatestOnlyHubLifetimeManager<TestHub>(
            new TypedCapturingLogger<LatestOnlyHubLifetimeManager<TestHub>>(logger),
            NullLogger<DefaultHubLifetimeManager<TestHub>>.Instance);
        await using var first = new TestConnection("dup", false);
        await using var second = new TestConnection("dup", false);
        await manager.OnConnectedAsync(first.Context);
        await manager.OnConnectedAsync(second.Context);

        var error = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains("ConnectionId=dup", error.Message);

        // 登録できなかった方の切断は、先に登録した接続のmailboxを外さない
        await manager.OnDisconnectedAsync(second.Context).WaitAsync(Timeout);
        await manager.SendAllLatestAsync("Receive", [5]);
        var received = await first.ReadUntilAsync(r => r.Count >= 1);
        Assert.Equal([("Receive", 5)], received);

        await manager.OnDisconnectedAsync(first.Context).WaitAsync(Timeout);
    }

    [Fact]
    public async Task Mailbox_詰まった接続は閾値超過時点で完了前にWarningが出る()
    {
        var logger = new CapturingLogger();
        var mailbox = new ConnectionMailbox("TestHub", logger, TimeSpan.FromMilliseconds(100));
        await using var blocked = new TestConnection("blocked", true);
        mailbox.Start(blocked.Context);

        mailbox.Post("Receive", new SerializedHubMessage(new InvocationMessage("Receive", [1])));

        // クライアントは読んでいない(書き込み未完了)状態でWarningが出る
        var deadline = DateTime.UtcNow + Timeout;
        while (!logger.Entries.Any(e => e.Level == LogLevel.Warning) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("送信中", warning.Message);
        Assert.Contains("ConnectionId=blocked", warning.Message);

        await mailbox.StopAsync().WaitAsync(Timeout);
    }

    [Fact]
    public async Task Mailbox_上書き破棄が起きた接続はConnectionIdと件数付きでDebugログが出る()
    {
        var logger = new CapturingLogger();
        var mailbox = new ConnectionMailbox("TestHub", logger);
        await using var blocked = new TestConnection("blocked", true);
        mailbox.Start(blocked.Context);

        // 1件目をpumpに取らせて書き込み待ちにしてから、続けてPostして上書きを起こす
        mailbox.Post("Receive", new SerializedHubMessage(new InvocationMessage("Receive", [1])));
        await Task.Delay(200);
        mailbox.Post("Receive", new SerializedHubMessage(new InvocationMessage("Receive", [2])));
        mailbox.Post("Receive", new SerializedHubMessage(new InvocationMessage("Receive", [3])));

        // クライアントが読み始めると送信が一巡し、上書きログが出る(30秒のレート制限なので最初の1回だけ)
        await blocked.ReadUntilAsync(r => r.Any(x => x.Value == 3));
        var deadline = DateTime.UtcNow + Timeout;
        while (!logger.Entries.Any(e => e.Level == LogLevel.Debug) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        var debug = Assert.Single(logger.Entries, e => e.Level == LogLevel.Debug);
        Assert.Contains("上書き破棄", debug.Message);
        Assert.Contains("ConnectionId=blocked", debug.Message);
        Assert.Matches(@"Count=[1-9]\d*", debug.Message);

        await mailbox.StopAsync().WaitAsync(Timeout);
    }

    [Fact]
    public async Task Mailbox_Postが流れ続けて送信が追いつかない間も上書き破棄のDebugログが出る()
    {
        var logger = new CapturingLogger();
        var mailbox = new ConnectionMailbox("TestHub", logger, TimeSpan.FromMilliseconds(100));
        await using var slow = new TestConnection("slow", true);
        mailbox.Start(slow.Context);

        // 1件目の送信を詰まらせる(閾値超過のWarningで、書き込み待ちに入ったことを確認する)
        mailbox.Post("Receive", Message("Receive", 0));
        var deadline = DateTime.UtcNow + Timeout;
        while (!logger.Entries.Any(e => e.Level == LogLevel.Warning) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);

        // 詰まっている間に送信より速くPostし続ける。以降はスロットが空にならず、pumpの内側ループが終わらない
        using var stop = new CancellationTokenSource();
        var value = 0;
        var feeder = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                mailbox.Post("Receive", Message("Receive", Interlocked.Increment(ref value)));
            }
        });
        while (Volatile.Read(ref value) < 10 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(1);
        }

        // クライアントが1回だけ読んで1件目の送信を完了させる。次の取り出しではスロットが埋まっており、
        // 2件目の書き込みでまた詰まるので、内側ループは空にならないまま止まる。この状態でログが出るかを見る
        await slow.ReadUntilAsync(r => r.Count >= 1);

        deadline = DateTime.UtcNow + Timeout;
        while (!logger.Entries.Any(e => e.Level == LogLevel.Debug) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        // Postを止めてから後始末する
        await stop.CancelAsync();
        await feeder.WaitAsync(Timeout);
        await mailbox.StopAsync().WaitAsync(Timeout);

        var debug = Assert.Single(logger.Entries, e => e.Level == LogLevel.Debug);
        Assert.Contains("上書き破棄", debug.Message);
        Assert.Contains("ConnectionId=slow", debug.Message);
    }

    [Fact]
    public async Task SendAllAsync_既存挙動は上書きされず全件届く()
    {
        var manager = CreateManager();
        await using var connection = new TestConnection("c", false);
        await manager.OnConnectedAsync(connection.Context);

        for (var i = 0; i < 20; i++)
        {
            await manager.SendAllAsync("Receive", [i]);
        }

        var received = await connection.ReadUntilAsync(r => r.Count >= 20);

        Assert.Equal(Enumerable.Range(0, 20), received.Select(x => x.Value));

        await manager.OnDisconnectedAsync(connection.Context);
    }

    [Fact]
    public async Task ILatestOnlySender_DIから解決するとLatestOnlyHubLifetimeManagerに届く()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(typeof(HubLifetimeManager<>), typeof(LatestOnlyHubLifetimeManager<>));
        services.AddSingleton(typeof(ILatestOnlySender<>), typeof(LatestOnlySender<>));
        await using var provider = services.BuildServiceProvider();

        var manager = provider.GetRequiredService<HubLifetimeManager<TestHub>>();
        Assert.IsType<LatestOnlyHubLifetimeManager<TestHub>>(manager);

        await using var connection = new TestConnection("c", false);
        await manager.OnConnectedAsync(connection.Context);

        var sender = provider.GetRequiredService<ILatestOnlySender<TestHub>>();
        await sender.SendAllLatestAsync("Receive", 7);

        var received = await connection.ReadUntilAsync(r => r.Count >= 1);
        Assert.Equal([("Receive", 7)], received);

        await manager.OnDisconnectedAsync(connection.Context);
    }

    [Fact]
    public async Task ILatestOnlySender_差し替え漏れ時は例外になる()
    {
        var sender = new LatestOnlySender<TestHub>(
            new DefaultHubLifetimeManager<TestHub>(NullLogger<DefaultHubLifetimeManager<TestHub>>.Instance));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAllLatestAsync("Receive", 1));
    }
}
