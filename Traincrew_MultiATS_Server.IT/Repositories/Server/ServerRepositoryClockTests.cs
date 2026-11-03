using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Repositories.Server;

namespace Traincrew_MultiATS_Server.IT.Repositories.Server;

/// <summary>既存server_stateの時差CASを使い捨てDBで検証する。</summary>
public sealed class ServerRepositoryClockTests
{
    private const int FirstTestStateId = 991_000;
    private const int SecondTestStateId = 991_001;

    /// <summary>期待値、1行制約、複数行ガードを実PostgreSQL上で確認する。</summary>
    [TtcSafePostgresFact]
    public async Task TrySetTimeOffset_UpdatesOnlyOneExpectedSingletonRow()
    {
        var rawConnectionString = Environment.GetEnvironmentVariable("TTC_TEST_CONNECTION")
            ?? throw new InvalidOperationException("TTC_TEST_CONNECTION is required.");
        var connectionString = ValidateConnection(rawConnectionString);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await EnsureServerStateTableAsync(connection);

        var existingRows = await CountRowsAsync(connection, $"id <> {FirstTestStateId} AND id <> {SecondTestStateId}");
        Assert.Equal(0L, existingRows);
        await DeleteTestRowsAsync(connection);
        try
        {
            await InsertStateAsync(connection, FirstTestStateId, 3);
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
            await using var context = new ApplicationDbContext(options);
            var repository = new ServerRepository(context);

            Assert.False(await repository.TrySetTimeOffsetAsync(expectedTimeOffset: 4, newTimeOffset: 7));
            Assert.Equal(3, await ReadOffsetAsync(connection, FirstTestStateId));
            Assert.True(await repository.TrySetTimeOffsetAsync(expectedTimeOffset: 3, newTimeOffset: 8));
            Assert.False(await repository.TrySetTimeOffsetAsync(expectedTimeOffset: 3, newTimeOffset: 9));
            Assert.Equal(8, await ReadOffsetAsync(connection, FirstTestStateId));

            await InsertStateAsync(connection, SecondTestStateId, 11);
            Assert.False(await repository.TrySetTimeOffsetAsync(expectedTimeOffset: 8, newTimeOffset: 12));
            Assert.Equal(8, await ReadOffsetAsync(connection, FirstTestStateId));
            Assert.Equal(11, await ReadOffsetAsync(connection, SecondTestStateId));
        }
        finally
        {
            await DeleteTestRowsAsync(connection);
        }
    }

    /// <summary>専用test DBに欠けたserver_stateだけを補う。</summary>
    private static async Task EnsureServerStateTableAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("""
            DO $$ BEGIN CREATE TYPE public.server_mode AS ENUM ('off', 'private', 'public'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
            CREATE TABLE IF NOT EXISTS public.server_state (
                id serial PRIMARY KEY,
                mode public.server_mode NOT NULL,
                time_offset integer NOT NULL DEFAULT 0,
                interlocking_heartbeat_at timestamp NULL DEFAULT NULL);
            """, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>expected値の比較だけでなく一意なserver_state行も必要とする。</summary>
    private static async Task InsertStateAsync(NpgsqlConnection connection, int id, int offset)
    {
        await using var command = new NpgsqlCommand(
            "INSERT INTO public.server_state (id, mode, time_offset) VALUES (@id, 'private', @offset)", connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("offset", offset);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>test用IDを持つserver_state行だけを後始末する。</summary>
    private static async Task DeleteTestRowsAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            "DELETE FROM public.server_state WHERE id IN (@first, @second)", connection);
        command.Parameters.AddWithValue("first", FirstTestStateId);
        command.Parameters.AddWithValue("second", SecondTestStateId);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>test行数を読み、他のDB状態を変更しない。</summary>
    private static async Task<long> CountRowsAsync(NpgsqlConnection connection, string predicate)
    {
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM public.server_state WHERE {predicate}", connection);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    /// <summary>期待値変更前後の単一フィールドを読み取る。</summary>
    private static async Task<int> ReadOffsetAsync(NpgsqlConnection connection, int id)
    {
        await using var command = new NpgsqlCommand("SELECT time_offset FROM public.server_state WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        return (int)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Test server_state row missing."));
    }

    /// <summary>専用test DB以外へ接続させない。</summary>
    internal static string ValidateConnection(string rawConnectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(rawConnectionString);
        if (!string.Equals(builder.Database, "ttc_test", StringComparison.Ordinal)
            || !string.Equals(builder.Username, "ttc_test", StringComparison.Ordinal))
            throw new InvalidOperationException("ServerRepository DB test requires Database=ttc_test and Username=ttc_test.");
        if (string.IsNullOrWhiteSpace(builder.Host))
            throw new InvalidOperationException("ServerRepository DB test requires a loopback Host.");
        var hosts = builder.Host.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0 || hosts.Any(host => !IsLoopback(host)))
            throw new InvalidOperationException("ServerRepository DB test requires a loopback Host.");
        return builder.ConnectionString;
    }

    /// <summary>ホストが127.0.0.1または::1へだけ解決されることを確認する。</summary>
    private static bool IsLoopback(string host)
    {
        if (IPAddress.TryParse(host, out var parsed)) return IPAddress.IsLoopback(parsed);
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            return addresses.Length > 0 && addresses.All(IPAddress.IsLoopback);
        }
        catch (SocketException) { return false; }
    }
}

/// <summary>TTC_TEST_CONNECTION未設定ならCAS DB検証を明示skipする。</summary>
public sealed class TtcSafePostgresFactAttribute : FactAttribute
{
    /// <summary>専用DB接続設定がない場合にだけテストをskipする。</summary>
    public TtcSafePostgresFactAttribute()
    {
        var connectionString = Environment.GetEnvironmentVariable("TTC_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            Skip = "TTC_TEST_CONNECTION is not configured; dedicated PostgreSQL test skipped.";
        else
            _ = ServerRepositoryClockTests.ValidateConnection(connectionString);
    }
}
