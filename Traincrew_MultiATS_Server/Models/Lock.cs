namespace Traincrew_MultiATS_Server.Models;
public class Lock
{
    public int Id { get; set; }
    public ulong ObjectId { get; set; }
    public string Type { get; set; }
    public LockCondition LockCondition { get; set; }
}

