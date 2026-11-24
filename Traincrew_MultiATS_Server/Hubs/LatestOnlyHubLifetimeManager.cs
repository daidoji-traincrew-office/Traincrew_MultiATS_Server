using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Traincrew_MultiATS_Server.Activity;

namespace Traincrew_MultiATS_Server.Hubs;

/// <summary>
/// 接続ごとのメールボックス。メソッドごとに最新の1件だけを保持し、pumpが順に送信する。
/// </summary>
/// <remarks>
/// 保持するのはメソッドごとに1スロットだけで、同じメソッドの未送信分は新しいもので上書きして捨てる。
/// 配信対象は全状態のスナップショットなので古い分は不要であり、
/// 送信が詰まってもメモリは「接続数×メソッド数」件で頭打ちになる。
/// 送信待ちメソッド名のリストは、上書きしても位置を保つ。
/// これにより同一pump内ではPostした順どおりに送られる。
/// ただしこの順序が保証されるのは同じmailbox内だけで、
/// 別スケジューラ間(同一ハブのReceiveDataとReceiveSignalData等)や通常のSendAllAsync(Default委譲)との順序は保証しない。
/// 共有状態はlockで保護し、送信(await)はlockの外で行う。
/// </remarks>
internal sealed class ConnectionMailbox(string hubName, ILogger logger, TimeSpan? slowWriteThreshold = null)
{
    // LatestOnlyHubLifetimeManager<THub>のstaticに置くとHubごとに別の計器が作られてしまう。
    // 非ジェネリックの本クラスに1つだけ持ち、Hubの区別はタグ(hub)で行う
    private static readonly Counter<long> OverwrittenCounter = ApplicationMetrics.Meter.CreateCounter<long>(
        "signalr.latest_only.overwritten",
        description: "latest-only配信で未送信のまま上書きされ破棄されたメッセージ数");

    // 引数はテストで閾値を縮めるためのもの
    private readonly TimeSpan _slowWriteThreshold = slowWriteThreshold ?? TimeSpan.FromSeconds(1);

    // 詰まった接続は毎ティック遅延扱いになるので、同じ接続のログはこの間隔で間引く
    private static readonly TimeSpan SlowWriteLogInterval = TimeSpan.FromSeconds(30);

    private readonly object _lock = new();

    // メソッド名 → 未送信の最新メッセージ(スロット)。上書きで古い分を捨てる
    private readonly Dictionary<string, SerializedHubMessage> _messagesByMethodName = new();

    // 送信待ちメソッド名を初回Post順に持つ。上書きでは並びを変えず、送信順を保つ
    private readonly List<string> _standbyMethodNames = [];

    // pumpの起床通知。データ本体はスロットにあり、ここは「何かある」を伝えるだけなので容量1(DropWrite)で足りる。
    // TryReadとTryTakeNextの間にPostが入っても、通知が1件残って空振りの1周が増えるだけで取りこぼしはない
    private readonly Channel<byte> _wakeupChannel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    private readonly CancellationTokenSource _cts = new();

    // ログのレート制限用。pumpの単一ループからしか触らないのでロック不要
    // 遅延(Warning/Information)・書き込み失敗(Error)・上書き破棄(Debug)で、間引きの状態を分ける。
    // 遅延と書き込み失敗で枠を共有すると、直前の遅延Warningで重大な書き込み失敗Errorが30秒抑止され得る
    private long _lastSlowWriteLogTimestamp;
    private long _lastWriteFailureLogTimestamp;
    private long _lastOverwriteLogTimestamp;

    // 前回の上書きログ以降に上書きで捨てた件数。Postはスケジューラの複数スレッドから呼ばれ得るのでInterlockedで触る
    private long _overwrittenSinceLastLog;
    private Task _pumpTask = Task.CompletedTask;

    /// <summary>
    /// テストから終了を観測するため。
    /// </summary>
    internal Task PumpTask => _pumpTask;

    /// <summary>
    /// メッセージをスロットに置く。同じメソッドの未送信メッセージがあれば上書きして破棄する(順序位置は保持)。
    /// </summary>
    /// <remarks>
    /// スケジューラのスレッドから呼ばれる。lock内は辞書とリストの操作だけにして、
    /// 詰まった接続がスケジューラを待たせないようにしている。
    /// </remarks>
    public void Post(string methodName, SerializedHubMessage message)
    {
        // 同じメソッドの未送信分があれば上書き、無ければ送信待ちの末尾に追加
        bool overwritten;
        lock (_lock)
        {
            overwritten = !_messagesByMethodName.TryAdd(methodName, message);
            if (overwritten)
            {
                _messagesByMethodName[methodName] = message;
            }
            else
            {
                _standbyMethodNames.Add(methodName);
            }
        }

        // 上書きで捨てた分を計上する(遅い接続の発見に使う)
        if (overwritten)
        {
            // メトリクスは遅い接続がいるかの検知用。接続IDをタグに入れるとカーディナリティが爆発するので入れない。
            // どの接続かの特定はpumpのログ(件数だけここで積む)で行う
            Interlocked.Increment(ref _overwrittenSinceLastLog);
            OverwrittenCounter.Add(1,
                new("hub", hubName),
                new("method", methodName));
        }

        // pumpを起こす。既に通知済みならDropWriteで捨てられる(通知は1件で足りる)
        _wakeupChannel.Writer.TryWrite(0);
    }

    /// <summary>
    /// 送信ループ(pump)を別タスクで起動する。
    /// </summary>
    public void Start(HubConnectionContext connection)
    {
        _pumpTask = Task.Run(() => PumpAsync(connection));
    }

    /// <summary>
    /// pumpを止めて終了を待つ。
    /// </summary>
    public async Task StopAsync()
    {
        // pumpに停止を伝える。CancelでWriteAsync待ちを、Completeで起床待ちを、それぞれ抜けさせる
        await _cts.CancelAsync();
        _wakeupChannel.Writer.TryComplete();
        try
        {
            // PumpAsyncは全例外を握るので例外は来ない
            await _pumpTask;
        }
        finally
        {
            // pumpが_cts.Tokenを使っている間に破棄するとObjectDisposedExceptionになるので、終了を待ってから破棄する
            _cts.Dispose();
        }
    }

    /// <summary>
    /// 送信待ちの先頭を1件だけ取り出し、そのスロットを空にする。取り出せるものが無ければfalse。
    /// </summary>
    /// <remarks>
    /// 全件を一括で取り出すと、1件目の送信が長引いた間にスロットが新しくなっても、
    /// 2件目以降は取り出した時点の古い値を送ってしまう。
    /// 1件ずつ取り出せば、各メソッドとも「送る直前のスロットの最新」を送れる。
    /// 取り出し後に届いたPostは、そのメソッドが空なら末尾に追加され、同じ周回の続きで送る。
    /// 送信(await)はlockの外で行うので、送信が詰まっている間もPostは待たされず、スロットの上書きだけが進む。
    /// </remarks>
    private bool TryTakeNext(out string methodName, out SerializedHubMessage message)
    {
        lock (_lock)
        {
            if (_standbyMethodNames.Count == 0)
            {
                methodName = "";
                message = null!;
                return false;
            }

            // 送信待ちの先頭を取り出し、スロットを空にする
            methodName = _standbyMethodNames[0];
            _standbyMethodNames.RemoveAt(0);
            message = _messagesByMethodName[methodName];
            _messagesByMethodName.Remove(methodName);
            return true;
        }
    }

    /// <summary>
    /// 起床通知を待ち、溜まったメッセージを順に接続へ書き込むループ。接続が切れるかStopされると終わる。
    /// </summary>
    /// <remarks>
    /// 1接続につき1本だけ走らせ、WriteAsyncの待機はこの接続の中で完結させる。
    /// 詰まった接続を自分から切ることはしない。読むのが遅いだけで生きているクライアントはpingが届くので、
    /// フレームワークのタイムアウトには掛からず残り得る。ただし保持するのはメソッドごとに最新1件なので、
    /// メモリは「接続数×メソッド数」件で有界であり、放置しても他に害はない。
    /// 切断するかどうかは、ログとメトリクスで遅い接続を観測してから判断する。
    /// </remarks>
    private async Task PumpAsync(HubConnectionContext connection)
    {
        // WriteAsyncの待機中にも切断を検知できるよう、接続のAbortedとStopの両方で止める
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, connection.ConnectionAborted);
        var token = linked.Token;
        try
        {
            // 起床通知が来るたびに、溜まっているものを空になるまで1件ずつ取り出して順に送る
            while (await _wakeupChannel.Reader.WaitToReadAsync(token))
            {
                // 通知を消費してから取り出す。逆順だと、取り出し中・消費前のPostの通知を消してしまい、
                // 取り出しが空になった後に入ったPostが次のPostまで送られない。
                // この順なら、消費後のPostは取り出しで拾うか、通知が残って次の周回で拾うかのどちらかになる
                _wakeupChannel.Reader.TryRead(out _);
                while (TryTakeNext(out var methodName, out var message))
                {
                    // 切断後に残りを送り続けないよう、メッセージごとに確認する
                    token.ThrowIfCancellationRequested();
                    await WriteAsync(connection, methodName, message, token);

                    // 内側ループの中で呼ぶ。遅い接続にPostが送信より速く流れ続けると内側ループが空にならず、
                    // ループの外では遅い接続に限って上書き破棄のログが一度も出なくなる。
                    // Debug無効・上書き0件なら即returnし、30秒のレート制限もあるので毎メッセージ呼んでも安い
                    TryLogOverwritten(connection);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 切断または停止
        }
        catch (System.Exception e)
        {
            // pumpが落ちるとその接続はlatest-onlyを受け取れなくなるので、握りつぶさず必ずログに残す
            logger.LogError(e, "latest-only pumpが異常終了しました ConnectionId={ConnectionId}",
                connection.ConnectionId);
            // pumpが死ぬとその接続は接続したまま定時配信が永久に止まり、クライアントは気付けない。
            // 切断させてクライアントの再接続で復旧させる
            connection.Abort();
        }
    }

    /// <summary>
    /// 1メッセージを接続へ書き込む。閾値を超えて詰まったら、完了を待たずに警告ログを出す。
    /// </summary>
    /// <remarks>
    /// 完了を待ってから所要時間を測ると、永遠に詰まった接続はログに出ず見つけられない。
    /// そのため閾値の時点でまだ完了していなければ、その場で1度出してから完了を待つ。
    /// </remarks>
    private async Task WriteAsync(HubConnectionContext connection, string methodName, SerializedHubMessage message,
        CancellationToken token)
    {
        var start = Stopwatch.GetTimestamp();
        var slowLogged = false;
        try
        {
            // 書き込みを開始する
            var valueTask = connection.WriteAsync(message, token);
            // 同期完了する通常ケースでは、Task化やDelayの確保を避ける
            if (valueTask.IsCompleted)
            {
                await valueTask;
            }
            else
            {
                // 閾値を超えたら送信中のうちに警告を出す(書き込み完了と閾値の早い方を待つ)
                var writeTask = valueTask.AsTask();
                using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                var delayTask = Task.Delay(_slowWriteThreshold, delayCts.Token);
                try
                {
                    if (await Task.WhenAny(writeTask, delayTask) == delayTask)
                    {
                        // tokenキャンセルでDelayが完了した場合はここでOCEが投げられる
                        await delayTask;
                        slowLogged = TryLogSlowWrite(LogLevel.Warning, connection, methodName,
                            "latest-only送信が閾値を超えても完了しません(送信中)", Stopwatch.GetElapsedTime(start));
                    }
                }
                finally
                {
                    // 書き込みが先に終わった場合にDelayを残さない
                    await delayCts.CancelAsync();
                }

                // 警告を出した後も、書き込みの完了(または失敗・キャンセル)までは待つ
                await writeTask;
            }
        }
        catch (OperationCanceledException)
        {
            // 下のcatchに握らせず、pumpのループまで伝えて終了させる
            throw;
        }
        catch (System.Exception e)
        {
            // HubConnectionContext.WriteAsyncは書き込みを内部ロックで直列化し、失敗時は自分で接続をAbortする(例外はほぼ外に出ない)。
            // Abort後のWriteAsyncは何もせず即returnするので、続けて書いてもフレームは壊れない。
            // AbortでConnectionAbortedが発火し、pumpは次のThrowIfCancellationRequested/WaitToReadAsyncで抜けるので、このcatchは防御用。
            // ここに来るのは想定外なので、Errorで出す。遅延ログとは間引きの枠を分け、重大なErrorが遅延Warningで抑止されないようにする。
            // 後始末はその接続の切断処理(OnDisconnectedAsync)に任せ、ここでpumpを止めて自前で後始末はしない
            if (TryAcquireLogSlot(ref _lastWriteFailureLogTimestamp, Stopwatch.GetTimestamp()))
            {
                logger.LogError(e,
                    "latest-only書き込みに失敗しました Hub={Hub} Method={Method} ConnectionId={ConnectionId}",
                    hubName, methodName, connection.ConnectionId);
            }

            return;
        }

        // 完了後に遅延していたらInformationで残す(詰まり続けている場合は送信中のWarningが既に出ている)。
        // 閾値前に出せなかった遅延(閾値直後に完了した場合など)をここで拾う。出し済みなら二重に出さない
        var elapsed = Stopwatch.GetElapsedTime(start);
        if (!slowLogged && elapsed >= _slowWriteThreshold)
        {
            TryLogSlowWrite(LogLevel.Information, connection, methodName, "latest-only送信が遅延しました(完了)", elapsed);
        }
    }

    /// <summary>
    /// 接続ごとに SlowWriteLogInterval に1回だけログを出す共通のレート制限
    /// </summary>
    /// <remarks>
    /// 状態はmailboxごと(=接続ごと)に持つので、1接続の連続ログが他の接続のログを潰さない。
    /// 呼ぶのはpumpの単一ループだけなので、Interlockedやlockは使わない。
    /// </remarks>
    private static bool TryAcquireLogSlot(ref long lastTimestamp, long now)
    {
        // 前回のログから間隔が空いていなければ出さない
        var last = lastTimestamp;
        if (last != 0 && Stopwatch.GetElapsedTime(last, now) < SlowWriteLogInterval)
        {
            return false;
        }

        // 出す場合は時刻を記録する
        lastTimestamp = now;
        return true;
    }

    /// <summary>
    /// 上書き破棄が起きていれば、その接続を特定するログを出す。
    /// </summary>
    /// <remarks>
    /// 遅延ログの閾値(1秒)より短い、250ms〜1秒ほどかかる接続は、上書きが起きても遅延ログに出ない。
    /// そのため上書きの件数を別に数え、レート制限の許す時だけログに出す。
    /// 枠が取れない間は件数を取り出さず、次のログまで累積する(Countは前回のログ以降の累計)。
    /// latest-onlyは上書きで捨てる前提の設計なので上書きは異常ではなく、Debugで出す。
    /// 傾向はメトリクス signalr.latest_only.overwritten で見て、接続の特定が必要な時だけDebugを有効にする。
    /// </remarks>
    private void TryLogOverwritten(HubConnectionContext connection)
    {
        // Debugが無効なら件数の取り出しもレート制限枠の消費もしない
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        // 上書きが無ければ枠を消費しない
        if (Interlocked.Read(ref _overwrittenSinceLastLog) <= 0
            || !TryAcquireLogSlot(ref _lastOverwriteLogTimestamp, Stopwatch.GetTimestamp()))
        {
            return;
        }

        var count = Interlocked.Exchange(ref _overwrittenSinceLastLog, 0);
        logger.LogDebug(
            "latest-only配信で未送信のまま上書き破棄しました Hub={Hub} ConnectionId={ConnectionId} User={User} Count={Count}",
            hubName, connection.ConnectionId, ResolveUser(connection), count);
    }

    // 原因クライアントの特定用。認証方式によりsubが無いことがあるので順に探す
    private static string? ResolveUser(HubConnectionContext connection)
    {
        return connection.User.FindFirst("sub")?.Value
               ?? connection.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? connection.UserIdentifier;
    }

    private bool TryLogSlowWrite(LogLevel level, HubConnectionContext connection, string methodName, string text,
        TimeSpan elapsed)
    {
        // レート制限に掛かったら出さない
        if (!TryAcquireLogSlot(ref _lastSlowWriteLogTimestamp, Stopwatch.GetTimestamp()))
        {
            return false;
        }

        var user = ResolveUser(connection);
        logger.Log(level,
            "{Text} Hub={Hub} Method={Method} ConnectionId={ConnectionId} User={User} Elapsed={ElapsedMs}ms",
            text, hubName, methodName, connection.ConnectionId, user, (long)elapsed.TotalMilliseconds);
        return true;
    }
}

/// <summary>
/// 通常の配信(SendAllAsync等)はDefaultHubLifetimeManagerへ委譲し、
/// SendAllLatestAsyncだけ接続ごとのメールボックス経由で「メソッドごとに最新1件」を送る。
/// </summary>
/// <remarks>
/// DefaultHubLifetimeManagerのSendAllAsyncは全接続のWriteAsyncをWhenAllで待つ。
/// 1接続でも詰まると完了せず、スケジューラは次のティックに進めず全員への配信が止まる。
/// これを避けるためHubLifetimeManager自体を差し替え、接続ごとのmailbox/pumpを
/// OnConnectedAsync/OnDisconnectedAsyncで生成・破棄している。
/// SendAllAsyncを含む既存のメソッドはすべてDefaultへ委譲した既存挙動のままで、
/// latest-onlyは呼び出し側がSendAllLatestAsync(ILatestOnlySender経由)を選んだ配信だけに掛かるopt-inである。
/// 現在、定時送信(各スケジューラのReceiveData/ReceiveSignalData/ReceiveServerMode)はすべてlatest-onlyを選んでいる。
/// latest-onlyと通常配信の間では送信順序が保証されないので、通常配信を新たに足すときは順序依存に注意すること。
/// </remarks>
public class LatestOnlyHubLifetimeManager<THub>(
    ILogger<LatestOnlyHubLifetimeManager<THub>> logger,
    ILogger<DefaultHubLifetimeManager<THub>> innerLogger)
    : HubLifetimeManager<THub>
    where THub : Hub
{
    // DIから受けずにnewする。HubLifetimeManager<>を本クラスに差し替えているので、DIから受けると自分自身を受けることになる
    private readonly DefaultHubLifetimeManager<THub> _inner = new(innerLogger);
    private readonly ILogger _logger = logger;
    private readonly string _hubName = typeof(THub).Name;
    private readonly ConcurrentDictionary<string, ConnectionMailbox> _mailboxByConnectionId = new();

    // 接続自身に自分のmailboxを持たせるキー。切断時に、辞書の同じConnectionIdの別mailboxではなく自分のものだけを外すために使う
    private const string MailboxItemKey = "LatestOnly.Mailbox";

    /// <summary>
    /// Defaultへ登録したうえで、この接続のmailboxとpumpを用意する。
    /// </summary>
    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        await _inner.OnConnectedAsync(connection);
        // この接続専用のmailboxを作って登録し、pumpを起動する
        var mailbox = new ConnectionMailbox(_hubName, _logger);
        // 登録できた時だけ起動する。登録できなかったmailboxのpumpを走らせると誰にも止められず残る
        if (_mailboxByConnectionId.TryAdd(connection.ConnectionId, mailbox))
        {
            // 登録できた接続にだけ紐付ける。切断時はこの紐付けがある接続だけが自分のmailboxを外す
            connection.Items[MailboxItemKey] = mailbox;
            mailbox.Start(connection);
        }
        else
        {
            // ConnectionIdが重複した異常系。この接続にはlatest-onlyが配信されないので、握りつぶさず残す
            _logger.LogError(
                "latest-only mailboxを登録できませんでした(ConnectionIdが重複) Hub={Hub} ConnectionId={ConnectionId}",
                _hubName, connection.ConnectionId);
        }
    }

    /// <summary>
    /// この接続のpumpを止めてmailboxを破棄し、Defaultからも登録を外す。
    /// </summary>
    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        // pumpの停止で何かあってもDefaultの登録解除は必ず行う。漏れると切断済み接続が全配信の対象に残り続ける
        try
        {
            // 登録できなかった接続はItemsに何も無いので何もしない。
            // 辞書からは同一インスタンスの時だけ外し、重複した別接続のmailboxを巻き込まない。
            // 二重に呼ばれても2回止めないよう取得と同時に外す。
            if (connection.Items.Remove(MailboxItemKey, out var item) && item is ConnectionMailbox mailbox)
            {
                _mailboxByConnectionId.TryRemove(KeyValuePair.Create(connection.ConnectionId, mailbox));
                await mailbox.StopAsync();
            }
        }
        finally
        {
            await _inner.OnDisconnectedAsync(connection);
        }
    }

    /// <summary>
    /// 全接続のメールボックスに置いて即returnする。送信は接続ごとのpumpが行う。
    /// </summary>
    /// <remarks>
    /// SerializedHubMessageは1つだけ作り、全接続で共有する。接続ごとには作り直さない。
    /// 実際のシリアライズはプロトコルごとに最初に書き込むpumpで1回だけ行われる(SerializedHubMessage内部でロックされる)。
    /// そのため渡したargsは、pumpが書き込み終えるまで変更・再利用してはならない。
    /// 送信の完了を待たず即returnするので、スケジューラは接続の状態に関係なく次のティックへ進める。
    /// </remarks>
    public Task SendAllLatestAsync(string methodName, object?[] args)
    {
        // 全接続で共有するメッセージを1つ作る(シリアライズは書き込み時に遅延実行される)
        var message = new SerializedHubMessage(new InvocationMessage(methodName, args));
        // 接続中の全mailboxに置く
        foreach (var mailbox in _mailboxByConnectionId.Values)
        {
            mailbox.Post(methodName, message);
        }

        return Task.CompletedTask;
    }

    // 以下はすべてDefaultHubLifetimeManagerへの素通しの委譲(既存挙動のまま)。
    // SendAllAsync / SendAllExceptAsyncもlatest-onlyにはしない。opt-inはSendAllLatestAsyncだけ。
    public override Task SendAllAsync(string methodName, object?[] args,
        CancellationToken cancellationToken = default)
        => _inner.SendAllAsync(methodName, args, cancellationToken);

    public override Task SendAllExceptAsync(string methodName, object?[] args,
        IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        => _inner.SendAllExceptAsync(methodName, args, excludedConnectionIds, cancellationToken);

    public override Task SendConnectionAsync(string connectionId, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
        => _inner.SendConnectionAsync(connectionId, methodName, args, cancellationToken);

    public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
        => _inner.SendConnectionsAsync(connectionIds, methodName, args, cancellationToken);

    public override Task SendGroupAsync(string groupName, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
        => _inner.SendGroupAsync(groupName, methodName, args, cancellationToken);

    public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
        => _inner.SendGroupsAsync(groupNames, methodName, args, cancellationToken);

    public override Task SendGroupExceptAsync(string groupName, string methodName, object?[] args,
        IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        => _inner.SendGroupExceptAsync(groupName, methodName, args, excludedConnectionIds, cancellationToken);

    public override Task SendUserAsync(string userId, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
        => _inner.SendUserAsync(userId, methodName, args, cancellationToken);

    public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
        => _inner.SendUsersAsync(userIds, methodName, args, cancellationToken);

    public override Task AddToGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = default)
        => _inner.AddToGroupAsync(connectionId, groupName, cancellationToken);

    public override Task RemoveFromGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = default)
        => _inner.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);

    public override Task<T> InvokeConnectionAsync<T>(string connectionId, string methodName, object?[] args,
        CancellationToken cancellationToken)
        => _inner.InvokeConnectionAsync<T>(connectionId, methodName, args, cancellationToken);

    public override Task SetConnectionResultAsync(string connectionId, CompletionMessage result)
        => _inner.SetConnectionResultAsync(connectionId, result);

    public override bool TryGetReturnType(string invocationId, [NotNullWhen(true)] out Type? type)
        => _inner.TryGetReturnType(invocationId, out type);
}
