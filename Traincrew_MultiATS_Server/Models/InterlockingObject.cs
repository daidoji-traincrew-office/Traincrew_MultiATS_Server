using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

/*
 * 連動に必要な装置を表す
 */
[Table("interlocking_object")]
public class InterlockingObject
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key] 
    public ulong Id { get; set; }
    public string  Name { get; set; }
    public ObjectType Type { get; set; }
    public string? Description { get; set; }
    public string? StationId { get; set; }
    /// マスタ部を丸ごと複製する。MemberwiseClone なので将来カラムが増えてもコピー漏れが起きない。
    /// 派生型は共変戻り値で override して具体型を返すこと。
    internal virtual InterlockingObject ShallowClone() => (InterlockingObject)MemberwiseClone();
}