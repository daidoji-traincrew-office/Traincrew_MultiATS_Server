using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("phone_call_session")]
public class PhoneCallSession
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("caller_number")]
    [Required]
    [MaxLength(20)]
    public required string CallerNumber { get; set; }

    [Column("caller_connection_id")]
    [Required]
    [MaxLength(256)]
    public required string CallerConnectionId { get; set; }

    [Column("target_number")]
    [Required]
    [MaxLength(20)]
    public required string TargetNumber { get; set; }

    [Column("target_connection_id")]
    [MaxLength(256)]
    public string? TargetConnectionId { get; set; }

    [Column("status")]
    [Required]
    public PhoneCallStatus Status { get; set; } = PhoneCallStatus.Calling;

    [Column("created_at", TypeName = "timestamp without time zone")]
    [Required]
    public DateTime CreatedAt { get; set; }

    [Column("ended_at", TypeName = "timestamp without time zone")]
    public DateTime? EndedAt { get; set; }
}
