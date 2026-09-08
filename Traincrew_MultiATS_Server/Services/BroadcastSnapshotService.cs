using System.Data;
using Traincrew_MultiATS_Server.Activity;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Transaction;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 配信系5ハブ分のデータをまとめたスナップショット。
/// 1ティック=1トランザクション=1スナップショットで作られるため、
/// 軌道回路と信号現示が定義上整合する(描画の因果順逆転が起きない)。
/// </summary>
public record BroadcastSnapshot(
    List<SignalData> Signals,
    DataToInterlocking Interlocking,
    DataToCommanderTable CommanderTable,
    ServerToATSDataBySchedule Train,
    ConstantDataToTID Tid,
    DataToCTCP Ctcp);

public interface IBroadcastSnapshotService
{
    Task<BroadcastSnapshot> BuildAsync();
}

public class BroadcastSnapshotService(
    ITransactionRepository transactionRepository,
    ICommonReadsBuilder commonReadsBuilder,
    ISignalService signalService,
    IInterlockingService interlockingService,
    ICTCPService ctcpService,
    ITIDService tidService,
    ICommanderTableService commanderTableService,
    ITrainService trainService) : IBroadcastSnapshotService
{
    public async Task<BroadcastSnapshot> BuildAsync()
    {
        using var snapshotActivity = ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot");

        // REPEATABLE READ: このトランザクション内の全読み取りが同一スナップショットを見る。
        // 軌道回路と信号現示(SignalRepository.GetSignalsForCalcIndication が
        // TrackCircuitState.IsShortCircuit を直接参照する)が同一時点になるため、
        // 描画の因果順逆転が原理的に起きなくなる。
        await using var tx = await transactionRepository.BeginTransactionAsync(IsolationLevel.RepeatableRead);

        CommonReads commonReads;
        using (var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot.CommonReads"))
        {
            commonReads = await commonReadsBuilder.BuildAsync();
            activity?.SetTag("trackCircuit.count", commonReads.TrackCircuits.Count);
        }

        List<SignalData> signals;
        using (var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot.Signals"))
        {
            signals = await signalService.CalcAllSignalIndication();
            activity?.SetTag("signal.count", signals.Count);
        }

        DataToInterlocking interlocking;
        using (ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot.Interlocking"))
        {
            interlocking = await interlockingService.BuildInterlockingDataAsync(commonReads);
        }

        DataToCTCP ctcp;
        using (ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot.Ctcp"))
        {
            ctcp = await ctcpService.BuildCtcpDataAsync(commonReads);
        }

        ConstantDataToTID tid;
        using (ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot.Tid"))
        {
            tid = tidService.BuildTidData(commonReads);
        }

        DataToCommanderTable commanderTable;
        using (ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot.CommanderTable"))
        {
            commanderTable = await commanderTableService.BuildCommanderTableDataAsync(commonReads);
        }

        ServerToATSDataBySchedule train;
        using (ActivitySources.Scheduler.StartActivity("Broadcast.Snapshot.Train"))
        {
            train = await trainService.BuildScheduleDataAsync(commonReads);
        }

        await tx.CommitAsync();

        snapshotActivity?.SetTag("trackCircuit.count", commonReads.TrackCircuits.Count);
        snapshotActivity?.SetTag("signal.count", signals.Count);

        return new BroadcastSnapshot(signals, interlocking, commanderTable, train, tid, ctcp);
    }
}
