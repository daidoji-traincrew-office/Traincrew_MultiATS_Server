using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

/// <summary>
/// 進路状態のうち、公開サーバーが参照する列だけを持つテーブル。
/// リレー系の列は連動側(Traincrew_Rendo_Server)専有のテーブルにある。
/// 注意: このテーブルは2プロセスが別々の列を書く。全カラム上書きは禁止。
/// </summary>
[Table("route_state")]
public class RouteState
{
    [Key]
    public ulong Id { get; init; }

    /// <summary>
    /// 信号制御リレー
    /// </summary>
    public RaiseDrop IsSignalControlRaised { get; set; }

    /// <summary>
    /// 進路確保中か
    /// </summary>
    [Column("is_route_secured")]
    public RaiseDrop IsRouteSecured { get; set; }

    /// <summary>
    /// CTC制御中か(CTC制御盤からの操作入力。公開サーバーが書き、連動サーバーが読む)
    /// </summary>
    [Column("is_ctc_controlled")]
    public RaiseDrop IsCtcControlled { get; set; }
}
