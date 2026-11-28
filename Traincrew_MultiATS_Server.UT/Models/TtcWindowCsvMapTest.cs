using System.ComponentModel;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.UT.Models;

public class TtcWindowCsvMapTest
{
    private const string Header = "名前,所属駅,番線,表示駅1,表示駅2,表示駅3,表示駅4,種類,対応軌道回路1,対応軌道回路2";

    // BaseCsvLoader と同じ設定で読む
    private static List<TtcWindowCsv> Read(params string[] lines)
    {
        var text = string.Join("\n", [Header, ..lines]);
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });
        csv.Context.RegisterClassMap<TtcWindowCsvMap>();
        return csv.GetRecords<TtcWindowCsv>().ToList();
    }

    [Fact]
    [DisplayName("番線の窓は、番線・種類・表示駅・軌道回路が読めること")]
    public void ShouldReadHomeTrackWindow()
    {
        var records = Read("TH76_TTC_Track1,TH76,1,TH75,,,,ホーム番線,TH76_5LAT,");

        var record = Assert.Single(records);
        Assert.Equal("TH76_TTC_Track1", record.Name);
        Assert.Equal("TH76", record.StationId);
        Assert.Equal("1", record.TrackNumber);
        Assert.Equal(TtcWindowType.HomeTrack, record.Type);
        Assert.Equal(["TH75"], record.DisplayStations);
        Assert.Equal(["TH76_5LAT"], record.TrackCircuits);
    }

    [Fact]
    [DisplayName("番線が空欄の窓は、TrackNumberが空文字になること")]
    public void ShouldReadBlankTrackNumberAsEmpty()
    {
        var records = Read("Window_Up,TH76,,TH75,TH77,,,上り閉塞,TH76_1T,");

        var record = Assert.Single(records);
        Assert.Equal("", record.TrackNumber);
        Assert.Equal(TtcWindowType.Up, record.Type);
        Assert.Equal(["TH75", "TH77"], record.DisplayStations);
        Assert.Equal(["TH76_1T"], record.TrackCircuits);
    }

    [Fact]
    [DisplayName("番線が「なし」の窓は、TrackNumberが空文字になること")]
    public void ShouldReadNashiTrackNumberAsEmpty()
    {
        var records = Read("Window_Down,TH76,なし,TH75,,,,下り閉塞,TH76_1T,");

        Assert.Equal("", Assert.Single(records).TrackNumber);
    }

    [Fact]
    [DisplayName("種類「入換番線」「本線入換」は、それぞれShuntTrack・Switchingとして読めること")]
    public void ShouldReadShuntTrackAndSwitchingTypes()
    {
        var records = Read(
            "Window_Shunt,TH76,5,,,,,入換番線,TH76_1T,",
            "Window_Main,TH76,T,,,,,本線入換,TH76_2T,");

        Assert.Equal(TtcWindowType.SwitchingTrack, records[0].Type);
        Assert.Equal(TtcWindowType.SwitchingMainTrack, records[1].Type);
    }

    [Theory]
    [InlineData("場内途中")]
    [InlineData("出発途中")]
    [DisplayName("種類「場内途中」「出発途中」は、HomeTrackとして読めること")]
    public void ShouldReadMidwayTypesAsHomeTrack(string type)
    {
        var records = Read($"Window_Midway,TH76,,,,,,{type},TH76_1T,");

        Assert.Equal(TtcWindowType.HomeTrack, Assert.Single(records).Type);
    }

    [Theory]
    [InlineData("番線")]
    [InlineData("上り")]
    [InlineData("下り")]
    [InlineData("入換")]
    [DisplayName("古い種類の呼び方は、読み込みで誤りになること")]
    public void ShouldRejectLegacyTypeNames(string type)
    {
        Assert.NotNull(Record.Exception(() => Read($"Window_Old,TH76,,,,,,{type},TH76_1T,")));
    }

    [Fact]
    [DisplayName("対応軌道回路1・2の両方に値がある場合、両方が読めること")]
    public void ShouldReadBothTrackCircuits()
    {
        var records = Read("Window_Switching,TH76,,TH75,,,,本線入換,TH76_1T,TH76_2T");

        Assert.Equal(["TH76_1T", "TH76_2T"], Assert.Single(records).TrackCircuits);
    }

    [Fact]
    [DisplayName("対応軌道回路2が空欄・なしの場合、読み飛ばされること")]
    public void ShouldSkipBlankOrNashiTrackCircuit()
    {
        var records = Read(
            "Window1,TH76,1,TH75,,,,ホーム番線,TH76_5LAT,",
            "Window2,TH76,2,TH75,,,,ホーム番線,TH76_5LBT,なし");

        Assert.Equal(["TH76_5LAT"], records[0].TrackCircuits);
        Assert.Equal(["TH76_5LBT"], records[1].TrackCircuits);
    }
}
