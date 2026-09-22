using System.ComponentModel;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;

namespace Traincrew_MultiATS_Server.UT.Initialization.DbInitializers;

public class ClosedCircuitLockTrackCircuitDbInitializerTest
{
    [Fact]
    [DisplayName("進路鎖錠欄・鎖錠欄・信号制御欄がTH76 3Rと同等の場合、鎖錠欄かつ進路鎖錠欄にあり信号制御欄にない軌道回路が返ること")]
    public void CalculateClosedCircuitLockTrackCircuitIds_TH76の3R相当_閉路鎖錠対象の軌道回路が返る()
    {
        // Arrange
        // 21ｲT=1, 23T=2, 25T=3, 26ｲT=4, 26ﾛT=5
        List<ulong> routeTcIds = [1, 2, 3, 4, 5];
        List<ulong> lockTcIds = [1];
        List<ulong> signalControlTcIds = [2, 3, 4, 5];

        // Act
        var actual = ClosedCircuitLockTrackCircuitDbInitializer.CalculateClosedCircuitLockTrackCircuitIds(
            routeTcIds, lockTcIds, signalControlTcIds);

        // Assert
        Assert.Equal(new HashSet<ulong> { 1 }, actual);
    }

    [Fact]
    [DisplayName("鎖錠欄に軌道回路が無い場合、空集合が返ること")]
    public void CalculateClosedCircuitLockTrackCircuitIds_鎖錠欄に軌道回路が無い_空集合が返る()
    {
        // Arrange
        List<ulong> routeTcIds = [2, 3];
        List<ulong> lockTcIds = [];
        List<ulong> signalControlTcIds = [2];

        // Act
        var actual = ClosedCircuitLockTrackCircuitDbInitializer.CalculateClosedCircuitLockTrackCircuitIds(
            routeTcIds, lockTcIds, signalControlTcIds);

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    [DisplayName("進路鎖錠欄が空の場合、空集合が返ること")]
    public void CalculateClosedCircuitLockTrackCircuitIds_進路鎖錠欄が空_空集合が返る()
    {
        // Arrange
        List<ulong> routeTcIds = [];
        List<ulong> lockTcIds = [1];
        List<ulong> signalControlTcIds = [];

        // Act
        var actual = ClosedCircuitLockTrackCircuitDbInitializer.CalculateClosedCircuitLockTrackCircuitIds(
            routeTcIds, lockTcIds, signalControlTcIds);

        // Assert
        Assert.Empty(actual);
    }
}
