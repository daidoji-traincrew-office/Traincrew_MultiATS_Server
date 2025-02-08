using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Models;

[Table("lock_condition_object")]
public class LockConditionObject: LockCondition
{
    public ulong ObjectId { get; set; }
    public InterlockingObject Object { get; set; }
    public int? TimerSeconds { get; set; }
    public NR IsReverse { get; set; }
    public bool IsSingleLock { get; set; }
}

