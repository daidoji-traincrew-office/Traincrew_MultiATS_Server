using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("server_state")]
public class ServerState
{
    public int Id { get; set; }
    public ServerMode Mode { get; set; }
    [Column("time_offset")]
    public int TimeOffset { get; set; } = 0;
    [Column("switch_move_time")]
    public int SwitchMoveTime { get; set; } = 0;
    [Column("switch_return_time")]
    public int SwitchReturnTime { get; set; } = 0;
    [Column("use_one_second_relay")]
    public bool UseOneSecondRelay { get; set; } = false;
}