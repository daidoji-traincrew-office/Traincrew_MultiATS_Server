using Moq;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// DateTimeService のユニットテスト
/// </summary>
public class DateTimeServiceTest
{
    // ヘルパーメソッド: DateTimeServiceのインスタンスを作成
    private static DateTimeService CreateDateTimeService(DateTime jstNow, int timeOffset)
    {
        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(r => r.GetNow()).Returns(jstNow);

        var mockServerRepository = new Mock<IServerRepository>();
        mockServerRepository.Setup(r => r.GetTimeOffset()).ReturnsAsync(timeOffset);

        return new DateTimeService(mockDateTimeRepository.Object, mockServerRepository.Object);
    }

    [Theory]
    [InlineData(4, 0, 0, 14400)] // 営業日開始ちょうど
    [InlineData(10, 0, 0, 36000)] // 範囲内はそのまま
    [InlineData(3, 59, 0, 100740)] // 4:00より前は前営業日の深夜(27:59)
    [InlineData(0, 0, 0, 86400)] // 0時は24時扱い
    [InlineData(24, 0, 0, 86400)] // 24時表記はそのまま同値
    [InlineData(25, 30, 0, 91800)] // 24時超えダイヤはそのまま(25:30)
    [InlineData(27, 59, 59, 100799)] // 範囲の上限直前
    [InlineData(28, 0, 0, 14400)] // 上限で折り返す(4:00)
    [InlineData(34, 0, 0, 36000)] // 24時間超えを折り返す(10:00)
    [InlineData(-1, 0, 0, 82800)] // 負の値も折り返す(23:00)
    public void NormalizeToServiceDay_VariousTimes_ReturnsNormalizedTime(
        int hours, int minutes, int seconds, double expectedSeconds)
    {
        // Arrange
        var time = new TimeSpan(hours, minutes, seconds);

        // Act
        var actual = DateTimeService.NormalizeToServiceDay(time);

        // Assert
        Assert.Equal(expectedSeconds, actual.TotalSeconds);
    }

    [Theory]
    [InlineData(2024, 1, 1, 21, 0, 13, 36000)] // JST21:00 + 13時間 = 翌10:00
    [InlineData(2024, 1, 1, 21, 0, -11, 36000)] // JST21:00 - 11時間 = 当日10:00(+13と一致)
    [InlineData(2024, 1, 1, 12, 0, 0, 43200)] // 時差なし、範囲内はそのまま
    [InlineData(2024, 1, 1, 2, 0, 0, 93600)] // 時差なし、4:00より前は前営業日扱い(26:00)
    public async Task GetTstNow_VariousJstTimesAndOffsets_ReturnsNormalizedServiceDaySeconds(
        int year, int month, int day, int hour, int minute, int timeOffset, double expectedSeconds)
    {
        // Arrange
        var jstNow = new DateTime(year, month, day, hour, minute, 0);
        var dateTimeService = CreateDateTimeService(jstNow, timeOffset);

        // Act
        var actual = await dateTimeService.GetTstNow();

        // Assert
        Assert.Equal(expectedSeconds, actual.TotalSeconds);
    }
}
