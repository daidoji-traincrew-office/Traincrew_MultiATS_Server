using System.Text;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.IT.TestUtilities;

/// <summary>
/// Helper class to create database snapshots for testing
/// </summary>
public static class DatabaseSnapshotHelper
{
    /// <summary>
    /// Creates a snapshot of the current database schema and data
    /// </summary>
    public static async Task<DatabaseSnapshot> CreateSnapshotAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = new DatabaseSnapshot();

        // Get all table names (excluding migrations and OpenIddict tables)
        var tableNames = await GetTableNamesAsync(context, cancellationToken);

        foreach (var tableName in tableNames)
        {
            var tableSnapshot = await CreateTableSnapshotAsync(context, tableName, cancellationToken);
            snapshot.Tables[tableName] = tableSnapshot;
        }

        return snapshot;
    }

    private static async Task<List<string>> GetTableNamesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
            AND table_type = 'BASE TABLE'
            AND table_name NOT LIKE '__EFMigrationsHistory'
            AND table_name NOT LIKE 'OpenIddict%'
            ORDER BY table_name;
            """;

        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = query;

        var tableNames = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    private static async Task<TableSnapshot> CreateTableSnapshotAsync(ApplicationDbContext context, string tableName, CancellationToken cancellationToken)
    {
        var tableSnapshot = new TableSnapshot
        {
            Name = tableName,
            RowCount = await GetRowCountAsync(context, tableName, cancellationToken),
            Schema = await GetTableSchemaAsync(context, tableName, cancellationToken)
        };

        return tableSnapshot;
    }

    private static async Task<int> GetRowCountAsync(ApplicationDbContext context, string tableName, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<string> GetTableSchemaAsync(ApplicationDbContext context, string tableName, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT column_name, data_type, is_nullable, column_default
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @tableName
            ORDER BY ordinal_position;
            """;

        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = query;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var schema = new StringBuilder();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var isNullable = reader.GetString(2);
            var columnDefault = reader.IsDBNull(3) ? null : reader.GetString(3);

            schema.AppendLine($"{columnName}|{dataType}|{isNullable}|{columnDefault}");
        }

        return schema.ToString();
    }

    /// <summary>
    /// Serializes a snapshot to a JSON-like format for comparison
    /// </summary>
    public static string SerializeSnapshot(DatabaseSnapshot snapshot)
    {
        var sb = new StringBuilder();

        foreach (var (tableName, tableSnapshot) in snapshot.Tables.OrderBy(t => t.Key))
        {
            sb.AppendLine($"Table: {tableName}");
            sb.AppendLine($"RowCount: {tableSnapshot.RowCount}");
            sb.AppendLine("Schema:");
            sb.AppendLine(tableSnapshot.Schema);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Compares two snapshots and returns the differences
    /// </summary>
    public static SnapshotComparison CompareSnapshots(DatabaseSnapshot expected, DatabaseSnapshot actual)
    {
        var comparison = new SnapshotComparison();

        // Check for missing tables
        var expectedTables = expected.Tables.Keys.ToHashSet();
        var actualTables = actual.Tables.Keys.ToHashSet();

        comparison.MissingTables = expectedTables.Except(actualTables).OrderBy(t => t).ToList();
        comparison.ExtraTables = actualTables.Except(expectedTables).OrderBy(t => t).ToList();

        // Compare common tables
        foreach (var tableName in expectedTables.Intersect(actualTables).OrderBy(t => t))
        {
            var expectedTable = expected.Tables[tableName];
            var actualTable = actual.Tables[tableName];

            if (expectedTable.RowCount != actualTable.RowCount)
            {
                comparison.RowCountDifferences[tableName] =
                    new RowCountDifference(expectedTable.RowCount, actualTable.RowCount);
            }

            if (expectedTable.Schema != actualTable.Schema)
            {
                comparison.SchemaDifferences[tableName] =
                    new SchemaDifference(expectedTable.Schema, actualTable.Schema);
            }
        }

        return comparison;
    }
}

/// <summary>
/// Represents a snapshot of the database
/// </summary>
public class DatabaseSnapshot
{
    public Dictionary<string, TableSnapshot> Tables { get; set; } = new();
}

/// <summary>
/// Represents a snapshot of a single table
/// </summary>
public class TableSnapshot
{
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string Schema { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of comparing two snapshots
/// </summary>
public class SnapshotComparison
{
    public List<string> MissingTables { get; set; } = new();
    public List<string> ExtraTables { get; set; } = new();
    public Dictionary<string, RowCountDifference> RowCountDifferences { get; set; } = new();
    public Dictionary<string, SchemaDifference> SchemaDifferences { get; set; } = new();

    public bool HasDifferences =>
        MissingTables.Count > 0 ||
        ExtraTables.Count > 0 ||
        RowCountDifferences.Count > 0 ||
        SchemaDifferences.Count > 0;

    public string GetDifferencesSummary()
    {
        if (!HasDifferences)
        {
            return "No differences found.";
        }

        var sb = new StringBuilder();

        if (MissingTables.Count > 0)
        {
            sb.AppendLine("Missing tables:");
            foreach (var table in MissingTables)
            {
                sb.AppendLine($"  - {table}");
            }
        }

        if (ExtraTables.Count > 0)
        {
            sb.AppendLine("Extra tables:");
            foreach (var table in ExtraTables)
            {
                sb.AppendLine($"  - {table}");
            }
        }

        if (RowCountDifferences.Count > 0)
        {
            sb.AppendLine("Row count differences:");
            foreach (var (table, diff) in RowCountDifferences)
            {
                sb.AppendLine($"  - {table}: Expected {diff.Expected}, Actual {diff.Actual}");
            }
        }

        if (SchemaDifferences.Count > 0)
        {
            sb.AppendLine("Schema differences:");
            foreach (var table in SchemaDifferences.Keys)
            {
                sb.AppendLine($"  - {table}: Schema mismatch");
            }
        }

        return sb.ToString();
    }
}

public record RowCountDifference(int Expected, int Actual);
public record SchemaDifference(string Expected, string Actual);
