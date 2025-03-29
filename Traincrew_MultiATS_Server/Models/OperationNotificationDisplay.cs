using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("operation_notification_display")]
public class OperationNotificationDisplay
{
    [Key]
    public required string Name { get; set; } // 告知機の名前 (Primary Key)
    public required string StationId { get; set; } // 所属する停車場
    public required bool IsUp { get; set; } // 上り
    public required bool IsDown { get; set; } // 下り
    public virtual OperationNotificationState? OperationNotificationState { get; set; } // 告知機の状態
}