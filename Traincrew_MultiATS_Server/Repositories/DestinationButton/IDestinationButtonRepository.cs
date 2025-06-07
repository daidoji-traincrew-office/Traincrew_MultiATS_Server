namespace Traincrew_MultiATS_Server.Repositories.DestinationButton;

public interface IDestinationButtonRepository
{
    /// <summary>
    /// すべての着点ボタンとその状態を取得する
    /// </summary>
    Task<List<Models.DestinationButton>> GetAllWithState();

    /// <summary>
    /// 名前を指定して着点ボタンを取得する
    /// </summary>
    /// <param name="name">着点ボタンの名前。</param>
    /// <returns>見つかった場合は着点ボタン、見つからない場合は null</returns>
    Task<Models.DestinationButton?> GetButtonByName(string name);

    /// <summary>
    /// 指定された駅IDに関連付けられた着点ボタンを取得する
    /// </summary>
    /// <param name="stationIds">駅 ID のリスト</param>
    /// <returns>着点ボタンのリスト</returns>
    Task<List<Models.DestinationButton>> GetButtonsByStationIds(List<string> stationIds);

    /// <summary>
    /// 圧下してから1秒超過した着点ボタンを元にもどす
    /// </summary>
    /// <param name="now">現在の時刻。</param>
    Task UpdateRaisedButtonsAsync(DateTime now);
}