using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("station")]
public class Station
{
    [Key] 
    public string Id { get; set; }
    public string Name { get; set; }
}