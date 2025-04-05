using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("operation_notification_state")]
public class OperationNotificationState
{
    [Key]
    public required string DisplayName { get; set; } // 告知器の名前 (Primary Key)
    public required OperationNotificationType Type { get; set; } // 告知種類
    public required string Content { get; set; } // 表示データ
    [Column(TypeName = "timestamp without time zone")]
    public required DateTime OperatedAt { get; set; } // 操作時刻
}