using Microsoft.AspNetCore.SignalR;

namespace Traincrew_MultiATS_Server.Activity;

public class ActivityHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // ref. https://github.com/dotnet/runtime/issues/65528#issuecomment-1068855998
        var currentActivity = System.Diagnostics.Activity.Current;
        System.Diagnostics.Activity.Current = null;
        try
        {
            using var activity = ActivitySources.Hubs.StartActivity(
                $"{invocationContext.Hub.GetType().Name}.{invocationContext.HubMethodName}");
            return await next(invocationContext);
        }
        finally
        {
            System.Diagnostics.Activity.Current = currentActivity;
        }
    }
}