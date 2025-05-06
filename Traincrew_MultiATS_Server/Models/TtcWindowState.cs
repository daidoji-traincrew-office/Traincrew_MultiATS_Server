using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

/// <summary>
/// 列番窓の状態
/// </summary>
[Table("ttc_window_state")]
public class TtcWindowState
{
    /// <summary>
    /// 列番窓の名前
    /// </summary>      
    [Key]
    [Column("name")]
    public string Name { get; init; }

    /// <summary>
    /// 列車番号
    /// </summary>
    [Column("train_number")]
    public string TrainNumber { get; set; }
}
