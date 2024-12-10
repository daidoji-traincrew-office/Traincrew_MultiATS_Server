using System.ComponentModel.DataAnnotations;

namespace Traincrew_MultiATS_Server.Models;

public class LockCondition
{
    [Key]
    public int Id { get; set; }
    public int? LockId { get; set; }
    public Lock? Lock { get; set; }
    public string Type { get; set; }
    public ulong? ObjectId { get; set; }
    public InterlockingObject? targetObject { get; set; }
    public int? TimerSeconds { get; set; }
    public bool IsReverse { get; set; }
    public bool IsTotalControl { get; set; }
    public bool IsSingleLock { get; set; }
}