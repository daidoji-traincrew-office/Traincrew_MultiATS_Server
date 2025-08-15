using System.Diagnostics;

namespace Traincrew_MultiATS_Server.Activity;
public static class ActivitySources
{
    public static readonly ActivitySource Scheduler = new("Traincrew_MultiATS_Server.Scheduler");
    public static readonly ActivitySource Hubs = new("Traincrew_MultiATS_Server.Hubs");
}
