using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

/*
 * 連動に必要な装置を表す
 */
[Table("interlocking_object")]
public abstract class InterlockingObject
{
    [Key] 
    public ulong Id { get; set; }
    public string? StationId { get; set; }
    public string  Name { get; set; }
    public string Type { get; set; }
}