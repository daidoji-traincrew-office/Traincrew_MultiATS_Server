namespace Traincrew_MultiATS_Server.Models;

public class TraincrewRole
{
    // 入鋏(運転会への参加資格。認可が有効なら、すべてのエンドポイントで必須)
    public required bool IsBeginner { get; init; }
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