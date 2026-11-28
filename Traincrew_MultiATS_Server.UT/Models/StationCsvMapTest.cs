using System.ComponentModel;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.UT.Models;

public class StationCsvMapTest
{
    // BaseCsvLoader と同じ設定で読む
    private static List<StationCsv> Read(params string[] lines)
    {
        var text = string.Join("\n", lines);
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });
        csv.Context.RegisterClassMap<StationCsvMap>();
        return csv.GetRecords<StationCsv>().ToList();
    }

    [Fact]
    [DisplayName("駅制御装置ブロックの列がある場合、値が読めること")]
    public void ShouldReadControlBlock()
    {
        var records = Read("駅ID,駅名,停車場？,旅客駅？,駅制御装置ブロック", "TH76,館浜,O,O,A");

        var record = Assert.Single(records);
        Assert.Equal("TH76", record.Id);
        Assert.Equal("A", record.ControlBlock);
    }

    [Fact]
    [DisplayName("駅制御装置ブロックの列が無い場合、nullになること")]
    public void ShouldReadNullControlBlock_WhenColumnIsMissing()
    {
        var records = Read("駅ID,駅名,停車場？,旅客駅？", "TH76,館浜,O,O");

        var record = Assert.Single(records);
        Assert.Equal("館浜", record.Name);
        Assert.Null(record.ControlBlock);
    }

    [Fact]
    [DisplayName("駅制御装置ブロックが空欄の場合、nullになること")]
    public void ShouldReadNullControlBlock_WhenCellIsEmpty()
    {
        var records = Read("駅ID,駅名,停車場？,旅客駅？,駅制御装置ブロック", "TH76,館浜,O,O,");

        var record = Assert.Single(records);
        Assert.Null(record.ControlBlock);
    }
}
