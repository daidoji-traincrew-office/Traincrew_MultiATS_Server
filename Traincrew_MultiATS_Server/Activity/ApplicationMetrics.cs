using System.Diagnostics.Metrics;

namespace Traincrew_MultiATS_Server.Activity;

public static class ApplicationMetrics
{
    public static readonly Meter Meter = new("Traincrew_MultiATS_Server.Application");
}