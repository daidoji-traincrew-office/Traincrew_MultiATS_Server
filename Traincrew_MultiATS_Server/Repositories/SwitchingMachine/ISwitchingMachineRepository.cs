namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

public interface ISwitchingMachineRepository
{
    /// <summary>
    /// 転てつ器とその状態を取得する
    /// </summary>
    Task<List<Models.SwitchingMachine>> GetSwitchingMachinesWithState();

    /// <summary>
    /// 転換中の転てつ器のIDを取得する
    /// </summary>
    Task<List<ulong>> GetIdsWhereMoving();

    /// <summary>
    /// 単独てこが倒れている転てつ器のIDを取得する
    /// </summary>
    Task<List<ulong>> GetIdsWhereLeverReversed();

    /// <summary>
    /// てこリレー回路が扛上している進路に対する転てつ器のIDを取得する
    /// </summary>
    Task<List<ulong>> GetIdsWhereLeverRelayRaised();
}
