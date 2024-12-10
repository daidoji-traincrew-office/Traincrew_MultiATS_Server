using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("switching_machine")]
public class SwitchingMachine : InterlockingObject
{
    public string TcName { get; set; }
}