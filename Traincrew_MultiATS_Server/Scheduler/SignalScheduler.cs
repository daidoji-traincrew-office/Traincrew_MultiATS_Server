using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class SignalScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var trainHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TrainHub, ITrainClientContract>>();
        var tidHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TIDHub, ITIDClientContract>>();
        var commanderTableHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<CommanderTableHub, ICommanderTableClientContract>>();
        var signalService = scope.ServiceProvider.GetRequiredService<SignalService>();

        var signalData = await signalService.CalcAllSignalIndication();

        await Task.WhenAll(
            trainHubContext.Clients.All.ReceiveSignalData(signalData),
            tidHubContext.Clients.All.ReceiveSignalData(signalData),
            commanderTableHubContext.Clients.All.ReceiveSignalData(signalData)
        );
    }
}
