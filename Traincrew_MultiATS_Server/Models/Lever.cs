using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lever")]
public class Lever
{
    [Key]
    public string Name { get; set; }
    public string StationId { get; set; }
    public LeverType Type { get; set; }
}
