using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lock")]
public class Lock
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key] 
    public ulong Id { get; set; }
    public ulong ObjectId { get; set; }
    public InterlockingObject Object { get; set; }
    public LockType Type { get; set; }
    public int? ApproachLockTime { get; set; }
    public int? RouteLockGroup { get; set; }
}
