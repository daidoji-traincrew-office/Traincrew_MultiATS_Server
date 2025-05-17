using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.Fixture;

namespace Traincrew_MultiATS_Server.IT.Hubs;

[Collection("WebApplication")]
public class InterlockingHubTest(WebApplicationFixture factory)
{
    // Enum for ServerType
    public enum ServerType
    {
        TrackCircuit, // 軌道回路
        Points, // 転てつ器
        Signals, // 信号機
        PhysicalLevers, // 物理てこ
        PhysicalKeyLevers, // 物理鍵てこ
        PhysicalButtons, // 着点ボタン
        Directions, // 方向てこ
        Retsubans, // 列番
        Lamps, // 表示灯
        Empty // 空
    }

    // Class to represent a row in the TSV file
    public class InterlockingData
    {
        public ServerType ServerType { get; set; }
        public required string ServerName { get; set; } = "";
        public string? PointNameA { get; set; }
        public string? PointNameB { get; set; }
        public string UniqueName { get; set; } = "";
        public string? DirectionName { get; set; }
        // Add other properties as needed
    }

    // Method to map Japanese ServerType to Enum
    private static ServerType MapServerType(string serverType)
    {
        return serverType switch
        {
            "軌道回路表示灯" => ServerType.TrackCircuit,
            "転てつ器表示灯" => ServerType.Points,
            "信号機表示灯" => ServerType.Signals,
            "物理てこ" => ServerType.PhysicalLevers,
            "物理鍵てこ" => ServerType.PhysicalKeyLevers,
            "着点ボタン" => ServerType.PhysicalButtons,
            "方向てこ表示灯" => ServerType.Directions,
            "列車番号" => ServerType.Retsubans,
            "状態表示灯" => ServerType.Lamps,
            "解放表示灯" or "駅扱切換表示灯" => ServerType.Empty, // 一旦仮でEmpty
            "" => ServerType.Empty,
            _ => throw new ArgumentException($"Unknown ServerType: {serverType}")
        };
    }

    [Fact]
    public async Task SendData_Interlocking_ValidatesDataFromTSVFiles()
    {
        // Arrange
        var tsvFiles = Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "Hubs", "InterlockingHubTestData"),
            "*.tsv"
        );

        var (connection, contract) = factory.CreateInterlockingHub();
        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);
            foreach (var tsvFile in tsvFiles)
            {
                var stationId = Path.GetFileName(tsvFile).Split('_')[0];

                var activeStationsList = new List<string> { stationId };

                using var reader = new StreamReader(tsvFile, Encoding.GetEncoding("Shift-jis"));
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = "\t",
                    HasHeaderRecord = true
                };
                using var csv = new CsvReader(reader, config);
                var expectedData = csv.GetRecords<dynamic>()
                    .Select(row => new InterlockingData
                    {
                        ServerType = MapServerType(row.ServerType),
                        ServerName = row.ServerName,
                        PointNameA = row.PointNameA,
                        PointNameB = row.PointNameB,
                        DirectionName = row.DirectionName,
                        UniqueName = row.UniqueName
                    })
                    .ToList();
                // Act
                var result = await contract.SendData_Interlocking(activeStationsList);

                // Assert
                // Todo: ホントは各PropertyがNullでなく、各要素の値が設定されていることを確認するべき
                AssertValidDataFromTsv(result, expectedData);
            }
        }
    }

    private static void AssertValidDataFromTsv(DataToInterlocking data, List<InterlockingData> expectedData)
    {
        var physicalLeverNames = data.PhysicalLevers.Select(l => l.Name).ToHashSet();
        var signalNames = data.Signals.Select(s => s.Name).ToHashSet();
        var physicalButtonNames = data.PhysicalButtons.Select(b => b.Name).ToHashSet();
        var directionNames = data.Directions.Select(d => d.Name).ToHashSet();
        var retsubanNames = data.Retsubans.Select(r => r.Name).ToHashSet();
        var trackCircuitNames = data.TrackCircuits.Select(tc => tc.Name).ToHashSet();
        var pointNames = data.Points.Select(p => p.Name).ToHashSet();
        var physicalKeyLeverNames = data.PhysicalKeyLevers.Select(k => k.Name).ToHashSet();
        var lampNames = data.Lamps.Keys.ToHashSet();

        var actions = expectedData
            .Where(IsMustAssertData)
            .Select<InterlockingData, Action>(row => () =>
                {
                    if (!string.IsNullOrWhiteSpace(row.ServerName))
                    {
                        switch (row.ServerType)
                        {
                            case ServerType.PhysicalLevers:
                                Assert.Contains(row.ServerName, physicalLeverNames);
                                break;
                            case ServerType.Signals:
                                Assert.Contains(row.ServerName, signalNames);
                                break;
                            case ServerType.PhysicalButtons:
                                Assert.Contains(row.ServerName, physicalButtonNames);
                                break;
                            case ServerType.Retsubans:
                                // Todo: 列番窓実装したらコメントアウト外す
                                // Assert.Contains(row.ServerName, retsubanNames);
                                break;
                            case ServerType.Lamps:
                                Assert.Contains(row.ServerName, lampNames);
                                break;
                            case ServerType.TrackCircuit:
                            case ServerType.Directions:
                                // 方向進路の場合、ServerNameには軌道回路名が入る
                                Assert.Contains(row.ServerName, trackCircuitNames);
                                break;
                            case ServerType.PhysicalKeyLevers:
                                Assert.Contains(row.ServerName, physicalKeyLeverNames);
                                break;
                            case ServerType.Points:
                            case ServerType.Empty:
                                // 転てつ器はPointNameAに書かれるのでここでは確認しない
                                // Emptyは確認しない
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    // PointNameAとPointNameBに転てつ器名が書かれている場合は、転てつ器名が存在することを確認
                    if (!string.IsNullOrWhiteSpace(row.PointNameA))
                    {
                        Assert.Contains(row.PointNameA, pointNames);
                    }

                    if (!string.IsNullOrWhiteSpace(row.PointNameB))
                    {
                        Assert.Contains(row.PointNameB, pointNames);
                    }

                    // 方向進路名が存在する場合は、方向進路名が存在することを確認 
                    if (!string.IsNullOrWhiteSpace(row.DirectionName))
                    {
                        Assert.Contains(row.DirectionName, directionNames);
                    }
                }
            )
            .ToArray();
        Assert.Multiple(actions);
    }

    /// <summary>
    /// テストケースの中から、確認しなければならないデータを選別する
    /// </summary>
    /// <param name="data">TSVの一列</param>
    /// <returns>そのデータをテストするべきか</returns>
    private static bool IsMustAssertData(InterlockingData data)
    {
        // 以下は実装してないので無視
        // CTC切換てこ
        // 転てつ不良表示灯
        return !data.UniqueName.StartsWith("駅扱切換") && !data.UniqueName.StartsWith("転てつ不良");
    }
}