using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Traincrew_MultiATS_Server.Models;

[Table("lock_condition")]
public class LockCondition
{
    [Key]
    public ulong Id { get; set; }
    public ulong LockId { get; set; }
    public virtual Lock? Lock { get; set; }
    public string Type { get; set; }
    public ulong? ObjectId { get; set; }
    public virtual InterlockingObject? targetObject { get; set; }
    public int? TimerSeconds { get; set; }
    public bool IsReverse { get; set; }
    public bool IsTotalControl { get; set; }
    public bool IsSingleLock { get; set; }
}