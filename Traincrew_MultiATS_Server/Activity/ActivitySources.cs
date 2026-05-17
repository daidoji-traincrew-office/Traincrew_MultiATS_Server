using System.Diagnostics;

namespace Traincrew_MultiATS_Server.Activity;
public static class ActivitySources
{
    public static readonly ActivitySource Scheduler = new("Traincrew_MultiATS_Server.Scheduler");
    public static readonly ActivitySource Hubs = new("Traincrew_MultiATS_Server.Hubs");

    // SignalRコネクションのHTTP Activityを親にすると、コネクションが切れるまでTraceがエクスポートされず
    // 同一trace_idに全Hub呼び出しがまとまる。呼び出し毎にRoot Activityにして独立Traceにする。
    public static System.Diagnostics.Activity? StartRootActivity(
        this ActivitySource source,
        string name,
        ActivityKind kind = ActivityKind.Server)
    {
        System.Diagnostics.Activity.Current = null;
        return source.StartActivity(name, kind);
    }
}
