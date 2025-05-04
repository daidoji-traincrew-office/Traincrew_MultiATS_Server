using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lock_condition_object")]
public class LockConditionObject: LockCondition
{
    public ulong ObjectId { get; set; }
    public InterlockingObject Object { get; set; }
    public int? TimerSeconds { get; set; }
    public NR IsReverse { get; set; }
    public bool IsSingleLock { get; set; }

    /// <summary>
    /// 方向てこの向き
    /// </summary>
    [Column("is_lr")]
    public LR? IsLR { get; set; }
}