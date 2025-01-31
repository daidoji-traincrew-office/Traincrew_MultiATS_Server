// ReSharper disable InconsistentNaming

using NpgsqlTypes;

namespace Traincrew_MultiATS_Server.Models;

public enum SignalIndication
{
    [PgName("R")]
    R = 1,
    [PgName("YY")]
    YY = 2,
    [PgName("Y")]
    Y = 3,
    [PgName("YG")]
    YG = 4,
    [PgName("G")]
    G = 5
}