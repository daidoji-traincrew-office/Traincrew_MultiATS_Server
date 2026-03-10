using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR;

namespace Traincrew_MultiATS_Server.Crew.Activity;

public class SignalRMetricsHubFilter : IHubFilter
{
    private readonly IMeterFactory _meterFactory;
    private readonly Meter _meter;
    private readonly UpDownCounter<long> _activeConnections;

    public SignalRMetricsHubFilter(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
        _meter = _meterFactory.Create("Microsoft.AspNetCore.Http.Connections");
        _activeConnections = _meter.CreateUpDownCounter<long>("signalr.server.active_connections", "connections", "Number of connections that are currently active on the server");
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        var tags = new TagList { { "hub.name", hubName } };

        _activeConnections.Add(1, tags);

        try
        {
            await next(context);
        }
        catch
        {
            _activeConnections.Add(-1, tags);
            throw;
        }
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, System.Exception? exception, Func<HubLifetimeContext, System.Exception?, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        var tags = new TagList { { "hub.name", hubName } };

        _activeConnections.Add(-1, tags);

        await next(context, exception);
    }
}
