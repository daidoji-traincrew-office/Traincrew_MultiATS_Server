using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.Transaction;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.IT.Service;

/// <summary>
/// 配信の読み取りをマスタ常駐+State取得に置き換えたことで、配信データが変わっていないことの差分テスト。
/// 旧経路(リポジトリJOIN・旧Serviceの取得メソッド・旧 CalcAllSignalIndication)と新経路
/// (CommonReadsBuilder・状態込みローダー・入力を渡す CalcAllSignalIndication)を、
/// 同じ読み取り専用スナップショット内で実行して、配信する5つのDTOと信号現示を比べる。
/// 旧経路は検証が終わるまで残す(旧メソッドは削除しない)。
/// </summary>
/// <remarks>
/// 並びはID昇順に変わっているので、比較は順序に依存しない(JSONを正規化して配列を並べ替える)。
/// DB上の各stateは、各シナリオで書き換えてから、最後に必ず元へ戻す。
/// </remarks>
[Collection("WebApplication")]
public class BroadcastReadsEquivalenceTest(WebApplicationFixture factory)
{
    /// <summary>書き換える列。バックアップ・復元に使う: (表, キー列, 値の列)</summary>
    private static readonly (string Table, string Key, string[] Columns)[] RestoreTargets =
    [
        ("track_circuit_state", "id", ["train_number", "is_short_circuit", "is_locked"]),
        ("direction_route_state", "id", ["is_lr"]),
        ("route_state", "id", ["is_signal_control_raised", "is_route_secured", "is_ctc_controlled"]),
        ("lever_state", "id", ["is_reversed"]),
        ("switching_machine_state", "id", ["is_switching", "is_reverse"]),
        ("route_central_control_lever_state", "id", ["is_inserted_key", "is_reversed", "is_center_controlled"]),
        ("direction_self_control_lever_state", "id", ["is_inserted_key", "is_reversed"]),
        ("destination_button_state", "name", ["is_raised"]),
        ("ttc_window_state", "name", ["train_number"]),
        ("station_timer_state", "id", ["is_timer_condition_met"]),
        ("server_state", "id", ["is_all_signal_relay_raised", "interlocking_heartbeat_at"])
    ];

    private const string Healthy =
        "UPDATE server_state SET is_all_signal_relay_raised = 'raise', interlocking_heartbeat_at = '{NOW}'";

    /// <summary>
    /// 全シナリオ共通: 空のDB状態(ベースライン)を含め、各シナリオを順に適用して比べる。
    /// </summary>
    private static readonly (string Name, string[] Sql)[] Scenarios =
    [
        ("ベースライン(変更なし・連動サーバー不健全)", []),
        ("連動サーバー健全のまま変更なし", [Healthy]),
        ("軌道回路の在線・鎖錠", [
            Healthy,
            "UPDATE track_circuit_state SET is_short_circuit = true, train_number = '回' || id WHERE id % 5 = 0",
            "UPDATE track_circuit_state SET is_locked = true WHERE id % 3 = 0"]),
        ("転てつ器・てこ・方向進路", [
            Healthy,
            "UPDATE switching_machine_state SET is_switching = true WHERE id % 4 = 0",
            "UPDATE switching_machine_state SET is_reverse = 'reversed' WHERE id % 3 = 0",
            "UPDATE lever_state SET is_reversed = 'right' WHERE id % 3 = 0",
            "UPDATE lever_state SET is_reversed = 'center' WHERE id % 7 = 0",
            "UPDATE direction_route_state SET is_lr = 'right' WHERE id % 2 = 0"]),
        ("進路(確保・信号制御・CTC)", [
            Healthy,
            "UPDATE route_state SET is_route_secured = 'raise' WHERE id % 4 = 0",
            "UPDATE route_state SET is_signal_control_raised = 'raise' WHERE id % 2 = 0",
            "UPDATE route_state SET is_ctc_controlled = 'raise' WHERE id % 3 = 0"]),
        ("進路集中制御てこ・方向自己制御てこ", [
            Healthy,
            "UPDATE route_central_control_lever_state SET is_center_controlled = true WHERE id % 2 = 0",
            "UPDATE route_central_control_lever_state SET is_inserted_key = true, is_reversed = 'reversed' WHERE id % 3 = 0",
            "UPDATE direction_self_control_lever_state SET is_inserted_key = true WHERE id % 2 = 0",
            "UPDATE direction_self_control_lever_state SET is_reversed = 'reversed' WHERE id % 3 = 0"]),
        ("着点ボタン・列番窓・時素", [
            Healthy,
            "UPDATE destination_button_state SET is_raised = 'raise' WHERE hashtext(name) % 3 = 0",
            "UPDATE ttc_window_state SET train_number = '列' || (hashtext(name) % 100) WHERE hashtext(name) % 2 = 0",
            "UPDATE station_timer_state SET is_timer_condition_met = true WHERE id % 2 = 0"]),
        ("信号現示が変化する(全進路の信号制御扛上+在線+方向)", [
            Healthy,
            "UPDATE route_state SET is_signal_control_raised = 'raise'",
            "UPDATE track_circuit_state SET is_short_circuit = true, train_number = '回' || id WHERE id % 6 = 0",
            "UPDATE direction_route_state SET is_lr = 'right' WHERE id % 2 = 0"]),
        ("連動サーバー不健全(全信号R)", [
            "UPDATE server_state SET is_all_signal_relay_raised = 'drop'"]),
        ("ハートビートが古い(全信号R)", [
            "UPDATE server_state SET is_all_signal_relay_raised = 'raise', interlocking_heartbeat_at = '{NOW-1MIN}'"])
    ];

    [Fact(DisplayName = "旧経路と新経路で、配信する5つのDTOと信号現示が一致すること(順序非依存)")]
    public async Task NewPathProducesSameBroadcastDataAsLegacyPath()
    {
        var ct = TestContext.Current.CancellationToken;
        await BackupAsync(ct);
        var distinctPhaseCounts = new List<int>();
        try
        {
            foreach (var (name, sqlList) in Scenarios)
            {
                await RestoreAsync(ct);
                await using (var scope = factory.Services.CreateAsyncScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    foreach (var sql in sqlList)
                    {
                        await context.Database.ExecuteSqlRawAsync(Resolve(sql), ct);
                    }
                }

                distinctPhaseCounts.Add(await CompareAsync(name, ct));
            }
        }
        finally
        {
            await RestoreAsync(CancellationToken.None);
            await DropBackupAsync(CancellationToken.None);
        }

        // 空振り防止: 信号現示が1種類(全R)しか出ないシナリオだけだと、現示計算の差分を検出できない
        Assert.Contains(distinctPhaseCounts, count => count >= 2);
    }

    /// <summary>
    /// ハートビートの時刻は、アプリ(dateTimeRepository.GetNow)と同じローカル時刻で書く
    /// (DBのnow()はタイムゾーンが違うことがある)。
    /// </summary>
    private static string Resolve(string sql)
    {
        const string format = "yyyy-MM-dd HH:mm:ss.ffffff";
        var now = DateTime.Now;
        return sql
            .Replace("{NOW-1MIN}", now.AddMinutes(-1).ToString(format))
            .Replace("{NOW}", now.ToString(format));
    }

    /// <returns>新経路の信号現示に含まれる現示の種類数</returns>
    private async Task<int> CompareAsync(string scenarioName, CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        // 本番と同じ読み取り専用スナップショット内で、旧・新を続けて読む(途中でDBが変わっても同じ値を見る)
        await using var tx = await sp.GetRequiredService<ITransactionRepository>()
            .BeginReadOnlyTransactionAsync(IsolationLevel.RepeatableRead, ct);

        var legacyReads = await BuildLegacyReadsAsync(sp);
        var newReads = await sp.GetRequiredService<ICommonReadsBuilder>().BuildAsync();

        var interlockingService = sp.GetRequiredService<IInterlockingService>();
        var tidService = sp.GetRequiredService<ITIDService>();
        var ctcpService = sp.GetRequiredService<ICTCPService>();
        var commanderTableService = sp.GetRequiredService<ICommanderTableService>();
        var trainService = sp.GetRequiredService<ITrainService>();
        var signalService = sp.GetRequiredService<ISignalService>();

        AssertSame(scenarioName, "DataToInterlocking",
            await interlockingService.BuildInterlockingDataAsync(legacyReads),
            await interlockingService.BuildInterlockingDataAsync(newReads));
        AssertSame(scenarioName, "ConstantDataToTID",
            tidService.BuildTidData(legacyReads), tidService.BuildTidData(newReads));
        AssertSame(scenarioName, "DataToCTCP",
            await ctcpService.BuildCtcpDataAsync(legacyReads), await ctcpService.BuildCtcpDataAsync(newReads));
        AssertSame(scenarioName, "DataToCommanderTable",
            await commanderTableService.BuildCommanderTableDataAsync(legacyReads),
            await commanderTableService.BuildCommanderTableDataAsync(newReads));
        AssertSame(scenarioName, "ServerToATSDataBySchedule",
            await trainService.BuildScheduleDataAsync(legacyReads), await trainService.BuildScheduleDataAsync(newReads));

        var legacySignals = await signalService.CalcAllSignalIndication();
        var newSignals = signalService.CalcAllSignalIndication(newReads.ServerState, newReads.SignalInput);
        AssertSame(scenarioName, "信号現示", legacySignals, newSignals);

        await tx.CommitAsync(ct);
        return newSignals.Select(signal => signal.phase).Distinct().Count();
    }

    /// <summary>
    /// 旧 CommonReadsBuilder(JOINを使う各リポジトリ・Serviceの取得メソッド)と同じ読み方。
    /// 新しい項目(てこ・方向自己制御てこ・着点ボタン・有効進路)は、旧InterlockingService/TrainServiceが
    /// 内部で呼んでいた取得メソッドで埋める。
    /// </summary>
    private static async Task<CommonReads> BuildLegacyReadsAsync(IServiceProvider sp)
    {
        var stationRepository = sp.GetRequiredService<IStationRepository>();
        var stationIds = (await stationRepository.GetWhereIsStation()).Select(station => station.Id).ToList();
        var serverState = ServerStateSnapshot.From(
            await sp.GetRequiredService<IServerRepository>().GetServerStateAsync());

        return new(
            stationIds,
            await sp.GetRequiredService<ITrackCircuitService>().GetAllTrackCircuitDataList(),
            await sp.GetRequiredService<ISwitchingMachineService>().GetAllSwitchData(),
            await sp.GetRequiredService<IDirectionRouteService>().GetAllDirectionData(),
            await sp.GetRequiredService<ITrainService>().GetAllTrainState(),
            await sp.GetRequiredService<IRouteCentralControlLeverRepository>().GetAllWithState(),
            await sp.GetRequiredService<ITtcStationControlService>().GetTtcWindowsByStationIdsWithState(stationIds),
            await stationRepository.GetTimerStatesByStationIds(stationIds),
            serverState,
            await sp.GetRequiredService<ILeverService>().GetAllLeverData(),
            await sp.GetRequiredService<IDirectionSelfControlLeverService>().GetAllWithState(),
            await sp.GetRequiredService<IDestinationButtonService>().GetAllButtonData(),
            await sp.GetRequiredService<IRouteService>().GetActiveRoutes(),
            // 旧経路の信号現示は CalcAllSignalIndication() が自分でDBを読むので使わない
            new([], [], []));
    }

    private static void AssertSame(string scenario, string what, object? legacy, object? current)
    {
        var legacyJson = Canonical(legacy);
        var currentJson = Canonical(current);
        Assert.True(
            legacyJson == currentJson,
            $"[{scenario}] {what} が旧経路と一致しない\n旧: {Truncate(legacyJson)}\n新: {Truncate(currentJson)}");
    }

    private static string Truncate(string json) => json.Length <= 2000 ? json : json[..2000] + "...";

    /// <summary>JSONに直し、オブジェクトのキー順と配列の要素順を正規化する(順序に依存しない比較)。</summary>
    private static string Canonical(object? value)
    {
        return Canonicalize(JsonSerializer.SerializeToNode(value))?.ToJsonString() ?? "null";
    }

    private static JsonNode? Canonicalize(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                var sorted = new JsonObject();
                foreach (var property in jsonObject.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    sorted[property.Key] = Canonicalize(property.Value);
                }

                return sorted;
            case JsonArray jsonArray:
                var items = jsonArray
                    .Select(Canonicalize)
                    .OrderBy(item => item?.ToJsonString() ?? "null", StringComparer.Ordinal)
                    .ToArray();
                return new JsonArray(items);
            default:
                return node?.DeepClone();
        }
    }

    // ---- バックアップ・復元(各シナリオ・終了時に元のDB状態へ戻す) ----
    // SQLに埋め込む表名・列名は、このクラスの定数 RestoreTargets だけ(外部入力ではない)ので、EF1002は抑止する
#pragma warning disable EF1002

    private async Task BackupAsync(CancellationToken ct)
    {
        await DropBackupAsync(ct);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var (table, _, _) in RestoreTargets)
        {
            await context.Database.ExecuteSqlRawAsync($"CREATE TABLE _it_bak_{table} AS SELECT * FROM {table}", ct);
        }
    }

    private async Task RestoreAsync(CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var (table, key, columns) in RestoreTargets)
        {
            var set = string.Join(", ", columns.Select(column => $"{column} = b.{column}"));
            await context.Database.ExecuteSqlRawAsync(
                $"UPDATE {table} AS t SET {set} FROM _it_bak_{table} AS b WHERE t.{key} = b.{key}", ct);
        }
    }

    private async Task DropBackupAsync(CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var (table, _, _) in RestoreTargets)
        {
            await context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS _it_bak_{table}", ct);
        }
    }

#pragma warning restore EF1002

    [Fact(DisplayName = "配信のスナップショットは読み取り専用で、トランザクション内の書き込みはDBが拒否すること")]
    public async Task ReadOnlyTransaction_RejectsWrites()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<ApplicationDbContext>();
        await using var tx = await sp.GetRequiredService<ITransactionRepository>()
            .BeginReadOnlyTransactionAsync(IsolationLevel.RepeatableRead, ct);

        // 読み取りは通る
        Assert.True(await context.ServerStates.AsNoTracking().CountAsync(ct) >= 0);

        // UPDATE も INSERT も 25006 read_only_sql_transaction で拒否される
        var update = await Assert.ThrowsAnyAsync<System.Exception>(() =>
            context.Database.ExecuteSqlRawAsync("UPDATE server_state SET time_offset = time_offset", ct));
        Assert.Equal("25006", FindPostgresException(update).SqlState);

        // 失敗したトランザクションはabortedになるので、INSERTは別の読み取り専用トランザクションで確かめる
        await tx.RollbackAsync(ct);
        await using var tx2 = await sp.GetRequiredService<ITransactionRepository>()
            .BeginReadOnlyTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var insert = await Assert.ThrowsAnyAsync<System.Exception>(() =>
            context.Database.ExecuteSqlRawAsync(
                "INSERT INTO ttc_window_state (name, train_number) VALUES ('_it_readonly_check', '')", ct));
        Assert.Equal("25006", FindPostgresException(insert).SqlState);
    }

    [Fact(DisplayName = "配信の1ティックを組み立てても、DbContextに未保存の変更が残らないこと")]
    public async Task BroadcastSnapshot_LeavesNoPendingChanges()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<ApplicationDbContext>();

        var snapshot = await sp.GetRequiredService<IBroadcastSnapshotService>().BuildAsync();

        Assert.NotNull(snapshot);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    private static PostgresException FindPostgresException(System.Exception exception)
    {
        for (var current = (System.Exception?)exception; current != null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

        }

        throw new InvalidOperationException("PostgresException が見つからない", exception);
    }
}
