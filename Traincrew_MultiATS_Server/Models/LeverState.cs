using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lever_state")]
public class LeverState
{
    [Key]
    public string Name { get; set; }
    public NRC IsReversed { get; set; }
}
