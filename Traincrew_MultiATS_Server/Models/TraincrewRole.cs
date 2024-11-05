namespace Traincrew_MultiATS_Server.Models;

public class TraincrewRole
{
    public const string ClaimType = "tc_role";
    public bool IsDriver { get; set; }
    public bool IsCommander { get; set; }
    public bool IsSignalman { get; set; }
}