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
        var tableNames = await context.Database
            .SqlQuery<string>(
                $"""
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_type = 'BASE TABLE'
                AND table_name NOT LIKE '__EFMigrationsHistory'
                AND table_name NOT LIKE 'OpenIddict%'
                ORDER BY table_name
                """)
            .ToListAsync(cancellationToken);

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
        // Note: tableName is passed as a parameter through FormattableString interpolation, preventing SQL injection
        FormattableString query = $"SELECT COUNT(*) FROM \"{tableName}\"";
        var result = await context.Database
            .SqlQuery<long>(query)
            .FirstAsync(cancellationToken);

        return Convert.ToInt32(result);
    }

    private static async Task<string> GetTableSchemaAsync(ApplicationDbContext context, string tableName, CancellationToken cancellationToken)
    {
        // Note: tableName is passed as a parameter through FormattableString interpolation, preventing SQL injection
        var columns = await context.Database
            .SqlQuery<ColumnInfo>(
                $"""
                SELECT column_name AS "ColumnName",
                       data_type AS "DataType",
                       is_nullable AS "IsNullable",
                       column_default AS "ColumnDefault"
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = {tableName}
                ORDER BY ordinal_position
                """)
            .ToListAsync(cancellationToken);

        var schema = new StringBuilder();
        foreach (var column in columns)
        {
            schema.AppendLine($"{column.ColumnName}|{column.DataType}|{column.IsNullable}|{column.ColumnDefault}");
        }

        return schema.ToString();
    }

    private record ColumnInfo(string ColumnName, string DataType, string IsNullable, string? ColumnDefault);

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
