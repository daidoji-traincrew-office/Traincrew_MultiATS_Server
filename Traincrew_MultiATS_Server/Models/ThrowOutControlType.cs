using System.Runtime.Serialization;

namespace Traincrew_MultiATS_Server.Models;

/// <summary>
/// 総括制御のタイプ
/// </summary>
public enum ThrowOutControlType
{
    [EnumMember(Value = "with_lever")]
    WithLever,

    [EnumMember(Value = "without_lever")]
    WithoutLever,

    [EnumMember(Value = "direction")]
    Direction
}
