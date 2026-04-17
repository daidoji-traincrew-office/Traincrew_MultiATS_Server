using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("diagram")]
public class Diagram
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("time_range")]
    public string TimeRange { get; set; } = string.Empty;

    [Required]
    [Column("version")]
    public string Version { get; set; } = string.Empty;
}
