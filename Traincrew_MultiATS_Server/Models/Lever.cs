using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lever")]
public class Lever
{
    [Key]
    public ulong Id { get; set; }
    public string Name { get; init; }
    public string StationId { get; init; }
    public LeverType Type { get; init; }
    public LeverState LeverState { get; init; }
}
