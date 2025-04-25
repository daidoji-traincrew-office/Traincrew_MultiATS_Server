using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("throw_out_control")]
public class ThrowOutControl
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public ulong Id { get; init; }
   
    /// <summary>
    /// 統括元オブジェクトID
    /// </summary>
    public ulong SourceId { get; init; }
    /// <summary>
    /// 統括元オブジェクト
    /// </summary>
    public InterlockingObject? Source { get; init; }
    /// <summary>
    /// 統括元が方向てこの場合、方向てこの向き
    /// </summary>
    public LR? SourceLr { get; init; }
    /// <summary>
    /// 統括先オブジェクトID
    /// </summary>
    public ulong TargetId { get; init; }
    /// <summary>
    /// 統括先オブジェクト
    /// </summary>
    public InterlockingObject? Target { get; init; }
    /// <summary>
    /// 統括先が方向てこの場合、方向てこの向き
    /// </summary>
    public LR? TargetLr { get; init; }
    /// <summary>
    /// てこ条件となる開放てこID
    /// </summary>
    public ulong? ConditionLeverId { get; init; }
    /// <summary>
    /// てこ条件となる開放てこ
    /// </summary>
    public OpeningLever? ConditionLever { get; init; }
    /// <summary>
    /// てこ条件の開放てこの向き
    /// </summary>
    public NR? ConditionNr { get; init; }
}