using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lever_state")]
public class LeverState
{
    [Key]
    public ulong Id { get; set; }
    public LCR IsReversed { get; set; }
}
