using Microsoft.Extensions.DependencyInjection;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.IT.TestUtilities;

namespace Traincrew_MultiATS_Server.IT.Initialization;

[Collection("WebApplication")]
public class DatabaseInitializerSnapshotTest(WebApplicationFixture factory)
{
    [Fact(Skip = "準備工事だけすませる", DisplayName = "空のDBから初期化した際に期待されるスナップショットと一致すること")]
    public async Task InitializeAsync_EmptyDatabase_MatchesExpectedSnapshot()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 初期化後のスナップショットを取得
        var actualSnapshot = await DatabaseSnapshotHelper.CreateSnapshotAsync(context, TestContext.Current.CancellationToken);

        // Assert - 期待されるスナップショットを読み込んで比較
        var expectedSnapshot = await LoadExpectedSnapshotAsync();
        var comparison = DatabaseSnapshotHelper.CompareSnapshots(expectedSnapshot, actualSnapshot);

        Assert.False(comparison.HasDifferences, $"Database snapshot mismatch:\n{comparison.GetDifferencesSummary()}");
    }

    [Fact(Skip = "This test is for manual snapshot generation only", DisplayName = "スナップショットファイルを生成する(手動実行用)")]
    public async Task GenerateExpectedSnapshot()
    {
        // このテストは手動で実行してスナップショットファイルを生成するためのもの
        // 通常のテスト実行ではスキップされる
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // スナップショットを取得
        var snapshot = await DatabaseSnapshotHelper.CreateSnapshotAsync(context, TestContext.Current.CancellationToken);

        // スナップショットをファイルに保存
        var snapshotPath = GetSnapshotFilePath();
        var serialized = DatabaseSnapshotHelper.SerializeSnapshot(snapshot);
        await File.WriteAllTextAsync(snapshotPath, serialized, TestContext.Current.CancellationToken);
    }

    private async Task<DatabaseSnapshot> LoadExpectedSnapshotAsync()
    {
        var snapshotPath = GetSnapshotFilePath();

        if (!File.Exists(snapshotPath))
        {
            throw new FileNotFoundException(
                $"Expected snapshot file not found at: {snapshotPath}\n" +
                "Please run the GenerateExpectedSnapshot test first to create the snapshot file.");
        }

        var content = await File.ReadAllTextAsync(snapshotPath, TestContext.Current.CancellationToken);
        return DeserializeSnapshot(content);
    }

    private static string GetSnapshotFilePath()
    {
        // テストプロジェクトのルートディレクトリ配下にスナップショットを保存
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", ".."));
        var snapshotDirectory = Path.Combine(projectRoot, "Initialization", "Snapshots");
        Directory.CreateDirectory(snapshotDirectory);
        return Path.Combine(snapshotDirectory, "expected_db_snapshot.txt");
    }

    private DatabaseSnapshot DeserializeSnapshot(string content)
    {
        var snapshot = new DatabaseSnapshot();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        TableSnapshot? currentTable = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("Table: "))
            {
                if (currentTable != null)
                {
                    snapshot.Tables[currentTable.Name] = currentTable;
                }

                currentTable = new TableSnapshot
                {
                    Name = line.Substring("Table: ".Length)
                };
            }
            else if (line.StartsWith("RowCount: ") && currentTable != null)
            {
                currentTable.RowCount = int.Parse(line.Substring("RowCount: ".Length));
            }
            else if (line.StartsWith("Schema:") && currentTable != null)
            {
                // スキーマ行以降をすべて読み込む
                var schemaLines = new List<string>();
                for (var j = i + 1; j < lines.Length; j++)
                {
                    var schemaLine = lines[j].Trim();
                    if (schemaLine.StartsWith("Table: "))
                    {
                        break;
                    }
                    if (!string.IsNullOrWhiteSpace(schemaLine))
                    {
                        schemaLines.Add(schemaLine);
                    }
                }
                currentTable.Schema = string.Join(Environment.NewLine, schemaLines) + Environment.NewLine;
            }
        }

        if (currentTable != null)
        {
            snapshot.Tables[currentTable.Name] = currentTable;
        }

        return snapshot;
    }
}
