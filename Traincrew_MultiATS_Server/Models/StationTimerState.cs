using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

/// <summary>
/// 注意: このテーブルは2プロセスが別々の列を書く。全カラム上書きは禁止。
/// </summary>
[Table("station_timer_state")]
public class StationTimerState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; init; }

    public required string StationId { get; set; }
    public required int Seconds { get; init; }

    /// <summary>
    /// 時素条件成立。
    /// 連動サーバーが書き、公開サーバーは読むのみ。
    /// </summary>
    [Column("is_timer_condition_met")]
    public bool IsTimerConditionMet { get; set; }
}
