using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

public class LockState
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key] 
    public ulong Id { get; set; }
    public ulong TargetObjectId { get; set; }
    public ulong SourceObjectId { get; set; }
    public NR IsReverse { get; set; }
    public LockType Type { get; set; }
    public DateTime? EndTime { get; set; }
}