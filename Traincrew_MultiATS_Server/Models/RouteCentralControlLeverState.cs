using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_central_control_lever_state")]
public class RouteCentralControlLeverState
{
    [Key]
    public ulong Id { get; init; }

    /// <summary>
    /// CHCリレー(進路中央制御レバー状態)
    /// </summary>
    public RaiseDrop IsChcRelayRaised { get; set; } = RaiseDrop.Drop;
}
