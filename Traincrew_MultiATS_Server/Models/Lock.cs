using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lock")]
public class Lock
{
    public ulong Id { get; set; }
    public ulong ObjectId { get; set; }
    public LockType Type { get; set; }
    public virtual LockCondition LockCondition { get; set; }
}

