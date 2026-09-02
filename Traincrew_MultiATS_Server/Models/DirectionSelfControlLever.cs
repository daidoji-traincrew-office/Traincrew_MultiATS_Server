using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_self_control_lever")]
public class DirectionSelfControlLever : InterlockingObject
{
    public DirectionSelfControlLeverState? DirectionSelfControlLeverState { get; set; }
}