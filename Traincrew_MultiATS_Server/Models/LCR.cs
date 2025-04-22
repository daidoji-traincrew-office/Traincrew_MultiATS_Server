using NpgsqlTypes;

namespace Traincrew_MultiATS_Server.Models;
// ReSharper disable once InconsistentNaming
public enum LCR
{
   [PgName("left")]
   Left,
   [PgName("center")]
   Center,
   [PgName("right")]
   Right,
}
    
