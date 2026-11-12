using Moq;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// DateTimeService のテスト。現在時刻は IDateTimeRepository の Mock で固定する。
/// </summary>
public class DateTimeServiceTest
{
    // 2026-11-12 03:00:00Z = JST 12:00:00
    private static readonly DateTimeOffset UtcNow = new(2026, 11, 12, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTime JstNow = new(2026, 11, 12, 12, 0, 0, DateTimeKind.Unspecified);

    private readonly Mock<IDateTimeRepository> _dateTimeRepositoryMock = new();
    private readonly Mock<IServerRepository> _serverRepositoryMock = new();
    private readonly DateTimeService _service;

    public DateTimeServiceTest()
    {
        _dateTimeRepositoryMock.Setup(r => r.GetUtcNow()).Returns(UtcNow);
        _service = new(_dateTimeRepositoryMock.Object, _serverRepositoryMock.Object);
    }

    [Fact]
    public async Task GetTstNow_AddsServerTimeOffsetToLocalNow()
    {
        _dateTimeRepositoryMock.Setup(r => r.GetNow()).Returns(JstNow);
        _serverRepositoryMock.Setup(r => r.GetTimeOffset()).ReturnsAsync(-3);

        var tst = await _service.GetTstNow();

        Assert.Equal(JstNow.AddHours(-3), tst);
    }

    [Fact]
    public void GetJstNow_ConvertsUtcNowToJapanTime()
    {
        Assert.Equal(JstNow, _service.GetJstNow());
    }

    // 負の値はハートビートが未来(ホスト間の時計のずれ)。健全として扱う。
    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1_999, true)]
    [InlineData(2_000, false)]
    public void IsInterlockingHeartbeatFresh_ChecksHeartbeatAge(int heartbeatAgeMilliseconds, bool expected)
    {
        var heartbeat = JstNow.AddMilliseconds(-heartbeatAgeMilliseconds);

        Assert.Equal(expected, _service.IsInterlockingHeartbeatFresh(heartbeat));
    }

    [Fact]
    public void IsInterlockingHeartbeatFresh_NoHeartbeat_ReturnsFalse()
    {
        Assert.False(_service.IsInterlockingHeartbeatFresh(null));
    }

    [Theory]
    [InlineData(-1, -1)]
    [InlineData(0, 0)]
    public void GetServiceDate_UsesFourOClockBoundary(long ticksFromBoundary, int dayOffset)
    {
        var tst = new DateTime(2026, 11, 4, 4, 0, 0, DateTimeKind.Unspecified).AddTicks(ticksFromBoundary);

        Assert.Equal(DateOnly.FromDateTime(tst.Date.AddDays(dayOffset)), DateTimeService.GetServiceDate(tst));
    }

    [Fact]
    public void ToTst_AddsOffsetHours()
    {
        Assert.Equal(JstNow.AddHours(5), DateTimeService.ToTst(JstNow, 5));
    }
}
