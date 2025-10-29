using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("server_state")]
public class ServerState
{
    public int Id { get; set; }
    public ServerMode Mode { get; set; }
    public RaiseDropWithForce IsAllSignalRelayRaised { get; set; } 
}