using Microsoft.Extensions.Logging;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;
using Traincrew_MultiATS_Server.Repositories.Train;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;
using Traincrew_MultiATS_Server.Services;
using Traincrew_MultiATS_Server.UT.Service.TestHelpers;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// TrainService.CalculateAndUpdateDelays メソッドのユニットテスト
/// </summary>
public class TrainServiceTest
{
    // ヘルパーメソッド: TrainServiceのインスタンスを作成
    private static TrainService CreateTrainService(
        TestTrackCircuitService? testTrackCircuitService = null,
        Mock<ITrainRepository>? mockTrainRepository = null,
        Mock<ITrainDiagramRepository>? mockTrainDiagramRepository = null,
        Mock<ITrackCircuitDepartmentTimeRepository>? mockTrackCircuitDepartmentTimeRepository = null,
        Mock<IDateTimeRepository>? mockDateTimeRepository = null,
        TestServerService? testServerService = null,
        Mock<ILogger<TrainService>>? mockLogger = null)
    {
        // nullの場合はデフォルトのインスタンスを使用
        testTrackCircuitService ??= new TestTrackCircuitService();
        mockTrainRepository ??= new Mock<ITrainRepository>();
        mockTrainDiagramRepository ??= new Mock<ITrainDiagramRepository>();
        mockTrackCircuitDepartmentTimeRepository ??= new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockDateTimeRepository ??= new Mock<IDateTimeRepository>();
        testServerService ??= new TestServerService();
        mockLogger ??= new Mock<ILogger<TrainService>>();

        // 使用しない依存関係はnull!で渡す（テスト対象メソッドで使用されない）
        return new TrainService(
            testTrackCircuitService,
            null!, // SignalService
            null!, // OperationNotificationService
            null!, // ProtectionService
            null!, // RouteService
            mockTrainRepository.Object,
            null!, // ITrainCarRepository
            mockTrainDiagramRepository.Object,
            null!, // ITransactionRepository
            null!, // BannedUserService
            null!, // IGeneralRepository
            testServerService,
            null!, // INextSignalRepository
            null!, // ITrainSignalStateRepository
            mockTrackCircuitDepartmentTimeRepository.Object,
            mockDateTimeRepository.Object,
            mockLogger.Object
        );
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_SingleStationWithDelay_UpdatesDelayCorrectly()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1002"; // 偶数 = 上り
        var carCount = 10;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit
        {
            Id = 100,
            Name = "TC1",
            StationId = "ST01",
            StationIdForDelay = "ST01"
        };

        var timetable = new TrainDiagramTimetable
        {
            Id = 1,
            Index = 2,
            StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(2))
        };

        var departmentTime = new TrackCircuitDepartmentTime
        {
            Id = 1,
            TrackCircuitId = 100,
            CarCount = 10,
            IsUp = true,
            TimeElement = 30 // 30秒の時素
        };

        // 現在時刻: 10:05:00 (発車予定 10:02:00 より 3分遅れ + 時素30秒 = 3.5分 → 3分)
        var currentTime = new DateTime(2024, 1, 1, 10, 5, 0);
        var timeOffset = 0;

        // Setup test helpers
        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository
            .Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01"))
            .ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository
            .Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 10))
            .ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository
            .Setup(x => x.GetNow())
            .Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(timeOffset));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository
            .Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var trainService = CreateTrainService(
            testTrackCircuitService,
            mockTrainRepository,
            mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository,
            mockDateTimeRepository,
            testServerService
        );

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert
        mockTrainRepository.Verify(
            x => x.SetDelayByTrainNumber(trainNumber, 3),
            Times.Once
        );
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_MultipleStations_UpdatesDelayForEachStation()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "2001"; // 奇数 = 下り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false },
            new() { Name = "TC2", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit1 = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var trackCircuit2 = new TrackCircuit { Id = 200, Name = "TC2", StationId = "ST02", StationIdForDelay = "ST02" };

        var timetable1 = new TrainDiagramTimetable
        {
            Id = 1, Index = 1, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(9), DepartureTime = TimeSpan.FromHours(9)
        };
        var timetable2 = new TrainDiagramTimetable
        {
            Id = 2, Index = 2, StationId = "ST02",
            ArrivalTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)),
            DepartureTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(32))
        };

        var departmentTime1 = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 8, IsUp = false, TimeElement = 20 };
        var departmentTime2 = new TrackCircuitDepartmentTime { Id = 2, TrackCircuitId = 200, CarCount = 8, IsUp = false, TimeElement = 25 };

        var currentTime = new DateTime(2024, 1, 1, 9, 35, 0);

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit1, trackCircuit2 }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable1);
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST02")).ReturnsAsync(timetable2);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, false, 8)).ReturnsAsync(departmentTime1);
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(200, false, 8)).ReturnsAsync(departmentTime2);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 2つの駅で遅延計算されるので2回呼ばれる
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_EarlyTrain_UpdatesWithNegativeDelay()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1004"; // 偶数 = 上り
        var carCount = 6;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(5))
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 6, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 10, 2, 0); // 発車予定より3分早い

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 6)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, -3), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_PassingThroughStation_UsesZeroCarCount()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "3002"; // 偶数 = 上り
        var carCount = 10;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 3, StationId = "ST01", // 始発駅でない
            ArrivalTime = TimeSpan.FromHours(11), // 到着時刻 = 出発時刻 (通過)
            DepartureTime = TimeSpan.FromHours(11)
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 0, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 11, 1, 0);

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 0)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 両数0で問い合わせることを確認
        mockTrackCircuitDepartmentTimeRepository.Verify(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 0), Times.Once);
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, 1), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_FirstStation_UsesActualCarCount()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "4001"; // 奇数 = 下り
        var carCount = 12;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 1, StationId = "ST01", // 始発駅
            ArrivalTime = TimeSpan.FromHours(8),
            DepartureTime = TimeSpan.FromHours(8) // 到着時刻 = 出発時刻でも始発なので実両数使用
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 12, IsUp = false, TimeElement = 15 };

        var currentTime = new DateTime(2024, 1, 1, 8, 2, 0);

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, false, 12)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 実両数12で問い合わせることを確認
        mockTrackCircuitDepartmentTimeRepository.Verify(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, false, 12), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_NoTimetableData_SkipsStation()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "5002"; // 偶数 = 上り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01"))
            .ReturnsAsync((TrainDiagramTimetable?)null); // 時刻表なし

        var mockTrainRepository = new Mock<ITrainRepository>();

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 時刻表がないのでSetDelayByTrainNumberは呼ばれない
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_NoDepartureTime_SkipsStation()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "6001"; // 奇数 = 下り
        var carCount = 6;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(12),
            DepartureTime = null // 出発時刻なし
        };

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrainRepository = new Mock<ITrainRepository>();

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 出発時刻がないのでSetDelayByTrainNumberは呼ばれない
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_NoDepartmentTime_LogsWarningAndUsesZeroTimeElement()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "7002"; // 偶数 = 上り
        var carCount = 10;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(14),
            DepartureTime = TimeSpan.FromHours(14).Add(TimeSpan.FromMinutes(2))
        };

        var currentTime = new DateTime(2024, 1, 1, 14, 5, 0);

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 10))
            .ReturnsAsync((TrackCircuitDepartmentTime?)null); // 出発時素なし

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<TrainService>>();

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService, mockLogger);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 警告がログ出力されることを確認
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("出発時素が見つかりませんでした")),
                It.IsAny<System.Exception>(),
                It.IsAny<Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);

        // 時素0で計算されるので、遅延は3分 (14:05 - 14:02)
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, 3), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_NonStationTrackCircuits_FiltersOutCorrectly()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "8001"; // 奇数 = 下り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false },
            new() { Name = "TC2", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit1 = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = null }; // 駅軌道回路でない
        var trackCircuit2 = new TrackCircuit { Id = 200, Name = "TC2", StationId = "ST02", StationIdForDelay = "ST02" }; // 駅軌道回路

        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST02",
            ArrivalTime = TimeSpan.FromHours(15),
            DepartureTime = TimeSpan.FromHours(15).Add(TimeSpan.FromMinutes(3))
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 200, CarCount = 8, IsUp = false, TimeElement = 10 };

        var currentTime = new DateTime(2024, 1, 1, 15, 5, 0);

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit1, trackCircuit2 }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST02")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(200, false, 8)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - TC1は駅軌道回路でないのでスキップ、TC2のみ処理される
        mockTrainDiagramRepository.Verify(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST02"), Times.Once);
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_UpTrain_PassesCorrectIsUpParameter()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "9002"; // 偶数 = 上り
        var carCount = 6;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(16),
            DepartureTime = TimeSpan.FromHours(16).Add(TimeSpan.FromMinutes(1))
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 6, IsUp = true, TimeElement = 5 };

        var currentTime = new DateTime(2024, 1, 1, 16, 2, 0);

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 6)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - isUp=trueで問い合わせることを確認
        mockTrackCircuitDepartmentTimeRepository.Verify(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 6), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_DownTrain_PassesCorrectIsUpParameter()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "9003"; // 奇数 = 下り
        var carCount = 6;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(16),
            DepartureTime = TimeSpan.FromHours(16).Add(TimeSpan.FromMinutes(1))
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 6, IsUp = false, TimeElement = 5 };

        var currentTime = new DateTime(2024, 1, 1, 16, 2, 0);

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, false, 6)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - isUp=falseで問い合わせることを確認
        mockTrackCircuitDepartmentTimeRepository.Verify(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, false, 6), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_EmptyTrackCircuitList_CompletesWithoutError()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1001";
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>(); // 空リスト

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit>()));

        var mockTrainRepository = new Mock<ITrainRepository>();

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - エラーなく完了し、何も処理されない
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_MidnightCrossingCurrentAfter_CalculatesDelayCorrectly()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1102"; // 偶数 = 上り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(58)),
            DepartureTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)) // 23:59発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 8, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 2, 0, 2, 0); // 翌日 00:02:00 (23:59発より3分遅れ)

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 8)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert
        // 営業日境界を考慮した正しい計算
        // 期待値: 3分遅れ (00:02 - 23:59 = 3分)
        // 営業日開始時刻(4:00)を基準に正規化:
        //   - 23:59は4:00以降なので: 86340秒
        //   - 00:02は4:00より前なので: 120 + 86400 = 86520秒
        //   - 遅延 = 86520 - 86340 = 180秒 = 3分
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, 3), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_MidnightCrossingCurrentBefore_CalculatesDelayCorrectly()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1204"; // 偶数 = 上り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromMinutes(2),
            DepartureTime = TimeSpan.FromMinutes(5) // 00:05発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 8, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 23, 58, 0); // 前日 23:58:00 (00:05発より7分早い = -7分)

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 8)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert
        // 営業日境界を考慮した正しい計算
        // 期待値: -7分早い (23:58 - 00:05 = -7分)
        // 営業日開始時刻(4:00)を基準に正規化:
        //   - 23:58は4:00以降なので: 86280秒
        //   - 00:05は4:00より前なので: 300 + 86400 = 86700秒
        //   - 遅延 = 86280 - 86700 = -420秒 = -7分
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, -7), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_WithPositiveTimeOffset_CalculatesDelayCorrectly()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1302"; // 偶数 = 上り
        var carCount = 10;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(2)) // 10:02発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 10, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 10, 3, 0); // 現在時刻: 10:03:00、timeOffset: +120秒
        var timeOffset = 120; // 実際の時刻: 10:03:00 + 120秒 = 10:05:00、遅延: 10:05:00 - 10:02:00 = 3分

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 10)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(timeOffset));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, 3), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_WithNegativeTimeOffset_CalculatesDelayCorrectly()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1404"; // 偶数 = 上り
        var carCount = 10;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(2)) // 10:02発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 10, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 10, 7, 0); // 現在時刻: 10:07:00、timeOffset: -120秒
        var timeOffset = -120; // 実際の時刻: 10:07:00 - 120秒 = 10:05:00、遅延: 10:05:00 - 10:02:00 = 3分

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 10)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(timeOffset));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, 3), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_PositiveDelayRounding_RoundsTowardZero()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1502"; // 偶数 = 上り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(2)) // 10:02:00発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 8, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 10, 4, 30); // 現在時刻: 10:04:30 (2分30秒遅れ = 2.5分)
                                                                 // MidpointRounding.ToZero: 2.5分 → 2分

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 8)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 2.5分 → ToZero丸めで2分
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, 2), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_NegativeDelayRounding_RoundsTowardZero()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1604"; // 偶数 = 上り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(5)) // 10:05:00発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 8, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 10, 2, 30); // 現在時刻: 10:02:30 (2分30秒早い = -2.5分)
                                                                 // MidpointRounding.ToZero: -2.5分 → -2分

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 8)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - -2.5分 → ToZero丸めで-2分
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, -2), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_PositiveDelayRoundingAlmostThreeMinutes_RoundsToTwo()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1702"; // 偶数 = 上り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(2)) // 10:02:00発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 8, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 10, 4, 59); // 現在時刻: 10:04:59 (2分59秒遅れ ≈ 2.98分)
                                                                 // MidpointRounding.ToZero: 2.98分 → 2分

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 8)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - 179秒 / 60 = 2.98分 → ToZero丸めで2分
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, 2), Times.Once);
    }

    [Fact]
    public async Task CalculateAndUpdateDelays_NegativeDelayRoundingAlmostThreeMinutes_RoundsToMinusTwo()
    {
        // Arrange
        var diaId = 1;
        var trainNumber = "1804"; // 偶数 = 上り
        var carCount = 8;
        var trackCircuitDataList = new List<TrackCircuitData>
        {
            new() { Name = "TC1", Last = trainNumber, On = true, Lock = false }
        };

        var trackCircuit = new TrackCircuit { Id = 100, Name = "TC1", StationId = "ST01", StationIdForDelay = "ST01" };
        var timetable = new TrainDiagramTimetable
        {
            Id = 1, Index = 2, StationId = "ST01",
            ArrivalTime = TimeSpan.FromHours(10),
            DepartureTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(5)) // 10:05:00発
        };
        var departmentTime = new TrackCircuitDepartmentTime { Id = 1, TrackCircuitId = 100, CarCount = 8, IsUp = true, TimeElement = 0 };

        var currentTime = new DateTime(2024, 1, 1, 10, 2, 1); // 現在時刻: 10:02:01 (2分59秒早い ≈ -2.98分)
                                                                // MidpointRounding.ToZero: -2.98分 → -2分

        var testTrackCircuitService = new TestTrackCircuitService();
        testTrackCircuitService.SetupGetTrackCircuitsByNames(_ => Task.FromResult(new List<TrackCircuit> { trackCircuit }));

        var mockTrainDiagramRepository = new Mock<ITrainDiagramRepository>();
        mockTrainDiagramRepository.Setup(x => x.GetTimetableByTrainNumberStationIdAndDiaId(diaId, trainNumber, "ST01")).ReturnsAsync(timetable);

        var mockTrackCircuitDepartmentTimeRepository = new Mock<ITrackCircuitDepartmentTimeRepository>();
        mockTrackCircuitDepartmentTimeRepository.Setup(x => x.GetByTrackCircuitIdAndIsUpAndMaxCarCount(100, true, 8)).ReturnsAsync(departmentTime);

        var mockDateTimeRepository = new Mock<IDateTimeRepository>();
        mockDateTimeRepository.Setup(x => x.GetNow()).Returns(currentTime);

        var testServerService = new TestServerService();
        testServerService.SetupGetTimeOffsetAsync(() => Task.FromResult(0));

        var mockTrainRepository = new Mock<ITrainRepository>();
        mockTrainRepository.Setup(x => x.SetDelayByTrainNumber(trainNumber, It.IsAny<int>())).Returns(Task.CompletedTask);

        var trainService = CreateTrainService(testTrackCircuitService, mockTrainRepository, mockTrainDiagramRepository,
            mockTrackCircuitDepartmentTimeRepository, mockDateTimeRepository, testServerService);

        // Act
        await trainService.CalculateAndUpdateDelays(diaId, trainNumber, carCount, trackCircuitDataList);

        // Assert - -179秒 / 60 = -2.98分 → ToZero丸めで-2分
        mockTrainRepository.Verify(x => x.SetDelayByTrainNumber(trainNumber, -2), Times.Once);
    }
}
