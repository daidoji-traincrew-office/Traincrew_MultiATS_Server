using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("user_disconnection_state")]
public class UserDisconnectionState
{
    public ulong UserId { get; set; }
}
