using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Hubs;

/// <summary>
/// BroadcastSchedulerの各ハブへの送り方(latest-only経路・連動盤の順序)のテスト。
/// </summary>
public class BroadcastSchedulerTest
{
    private static BroadcastSnapshot CreateSnapshot()
    {
        return new(
            Signals: [],
            Interlocking: new(),
            CommanderTable: new(),
            Train: new(),
            Tid: new(),
            Ctcp: new());
    }

    [Fact]
    public async Task SendInterlockingAsync_ReceiveData_ReceiveSignalDataの順にlatestOnlyで送る()
    {
        var snapshot = CreateSnapshot();
        var sender = new Mock<ILatestOnlySender<InterlockingHub>>(MockBehavior.Strict);
        var calls = new List<string>();
        sender.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Callback((string method, object?[] _) => calls.Add(method))
            .Returns(Task.CompletedTask);

        await BroadcastScheduler.SendInterlockingAsync(sender.Object, snapshot, NullLogger.Instance);

        Assert.Equal(
            [nameof(IInterlockingClientContract.ReceiveData), nameof(IInterlockingClientContract.ReceiveSignalData)],
            calls);
        sender.Verify(s => s.SendAllLatestAsync(nameof(IInterlockingClientContract.ReceiveData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Interlocking))), Times.Once);
        sender.Verify(s => s.SendAllLatestAsync(nameof(IInterlockingClientContract.ReceiveSignalData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Signals))), Times.Once);
    }

    [Fact]
    public async Task SendInterlockingAsync_送信が失敗しても例外を外へ出さない()
    {
        var sender = new Mock<ILatestOnlySender<InterlockingHub>>();
        sender.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await BroadcastScheduler.SendInterlockingAsync(sender.Object, CreateSnapshot(), NullLogger.Instance);
    }

    [Fact]
    public async Task SendTidAsync_ReceiveDataとReceiveSignalDataをlatestOnlyで送る()
    {
        var snapshot = CreateSnapshot();
        var sender = new Mock<ILatestOnlySender<TIDHub>>();
        sender.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>())).Returns(Task.CompletedTask);

        await BroadcastScheduler.SendTidAsync(sender.Object, snapshot, NullLogger.Instance);

        sender.Verify(s => s.SendAllLatestAsync(nameof(ITIDClientContract.ReceiveData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Tid))), Times.Once);
        sender.Verify(s => s.SendAllLatestAsync(nameof(ITIDClientContract.ReceiveSignalData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Signals))), Times.Once);
    }

    [Fact]
    public async Task SendCommanderTableAsync_ReceiveDataとReceiveSignalDataをlatestOnlyで送る()
    {
        var snapshot = CreateSnapshot();
        var sender = new Mock<ILatestOnlySender<CommanderTableHub>>();
        sender.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>())).Returns(Task.CompletedTask);

        await BroadcastScheduler.SendCommanderTableAsync(sender.Object, snapshot, NullLogger.Instance);

        sender.Verify(s => s.SendAllLatestAsync(nameof(ICommanderTableClientContract.ReceiveData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.CommanderTable))), Times.Once);
        sender.Verify(s => s.SendAllLatestAsync(nameof(ICommanderTableClientContract.ReceiveSignalData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Signals))), Times.Once);
    }

    [Fact]
    public async Task SendCtcpAsync_ReceiveDataだけをlatestOnlyで送る()
    {
        var snapshot = CreateSnapshot();
        var sender = new Mock<ILatestOnlySender<CTCPHub>>();
        sender.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>())).Returns(Task.CompletedTask);

        await BroadcastScheduler.SendCtcpAsync(sender.Object, snapshot, NullLogger.Instance);

        sender.Verify(s => s.SendAllLatestAsync(nameof(ICTCPClientContract.ReceiveData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Ctcp))), Times.Once);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendTrainAsync_ReceiveDataとReceiveSignalDataをlatestOnlyで送る()
    {
        var snapshot = CreateSnapshot();
        var sender = new Mock<ILatestOnlySender<TrainHub>>();
        sender.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>())).Returns(Task.CompletedTask);

        await BroadcastScheduler.SendTrainAsync(sender.Object, snapshot, NullLogger.Instance);

        sender.Verify(s => s.SendAllLatestAsync(nameof(ITrainClientContract.ReceiveData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Train))), Times.Once);
        sender.Verify(s => s.SendAllLatestAsync(nameof(ITrainClientContract.ReceiveSignalData),
            It.Is<object?[]>(a => ReferenceEquals(a.Single(), snapshot.Signals))), Times.Once);
    }

    [Fact]
    public async Task 一つのハブの送信が失敗しても他のハブの送信は行われる()
    {
        var snapshot = CreateSnapshot();
        var tid = new Mock<ILatestOnlySender<TIDHub>>();
        tid.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var train = new Mock<ILatestOnlySender<TrainHub>>();
        train.Setup(s => s.SendAllLatestAsync(It.IsAny<string>(), It.IsAny<object?[]>())).Returns(Task.CompletedTask);

        await Task.WhenAll(
            BroadcastScheduler.SendTidAsync(tid.Object, snapshot, NullLogger.Instance),
            BroadcastScheduler.SendTrainAsync(train.Object, snapshot, NullLogger.Instance));

        train.Verify(s => s.SendAllLatestAsync(nameof(ITrainClientContract.ReceiveData), It.IsAny<object?[]>()),
            Times.Once);
    }
}
