using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("user_disconnection_state")]
public class UserDisconnectionState
{
    [Key]
    public ulong UserId { get; set; }
}
