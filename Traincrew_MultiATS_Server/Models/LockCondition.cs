using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lock_condition")]
public class LockCondition
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public ulong Id { get; init; }
    public Lock? Lock { get; set; }
    public ulong LockId { get; set; }
    public LockCondition? Parent { get; set; }
    public ulong? ParentId { get; set; }
    public LockConditionType Type { get; set; }
}