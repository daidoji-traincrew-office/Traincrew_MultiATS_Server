using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("station")]
public class Station
{
    [Key] 
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsStation { get; init; }
    public required bool IsPassengerStation { get; init; }
    /// <summary>駅制御装置ブロック。同じ値の駅は同じブロックに属する。未設定は null</summary>
    public string? ControlBlock { get; set; }
}