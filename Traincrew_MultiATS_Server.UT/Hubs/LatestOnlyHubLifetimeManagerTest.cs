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

    [Fact]
    public async Task OnDisconnectedAsync_切断後はpumpが終了し_送信しても例外にならず何も書かれない()
    {
        var manager = CreateManager();
        await using var connection = new TestConnection("c", false);
        await manager.OnConnectedAsync(connection.Context);

        await manager.SendAllLatestAsync("Receive", [1]);
        var first = await connection.ReadUntilAsync(r => r.Count >= 1);
        Assert.Equal([("Receive", 1)], first);

        await manager.OnDisconnectedAsync(connection.Context).WaitAsync(Timeout);

        await manager.SendAllLatestAsync("Receive", [2]);
        await Task.Delay(300);

        Assert.False(connection.HasPendingData());
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
        await sender.SendAllAsync("Receive", 7);

        var received = await connection.ReadUntilAsync(r => r.Count >= 1);
        Assert.Equal([("Receive", 7)], received);

        await manager.OnDisconnectedAsync(connection.Context);
    }

    [Fact]
    public async Task ILatestOnlySender_差し替え漏れ時は例外になる()
    {
        var sender = new LatestOnlySender<TestHub>(
            new DefaultHubLifetimeManager<TestHub>(NullLogger<DefaultHubLifetimeManager<TestHub>>.Instance));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAllAsync("Receive", 1));
    }
}
