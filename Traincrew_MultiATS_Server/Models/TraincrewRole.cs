namespace Traincrew_MultiATS_Server.Models;

public class TraincrewRole
{
    // 運転士 
    public required bool  IsDriver { get; init; }
    // 乗務助役
    public required bool IsDriverManager { get; init; }
    // 車掌
    public required bool IsConductor { get; init; }
    // 司令員
    public required bool IsCommander { get; init; }
    // 信号扱者
    public required bool IsSignalman { get; init; }
    // 司令主任
    public required bool IsAdministrator { get; init; }
}