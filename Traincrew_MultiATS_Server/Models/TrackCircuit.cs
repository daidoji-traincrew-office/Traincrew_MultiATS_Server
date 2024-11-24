using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Traincrew_MultiATS_Server.Models;

[Table("circuit")]
public class Circuit
{
    [Key]
    public string Name { get; set; }
    [Column("DiaName")]
    public string DiaName { get; set; }
    [Column("BougoZone")]
    public int BougoZone { get; set; }
    [Column("isLock")]
    public bool isLock { get; set; }
}