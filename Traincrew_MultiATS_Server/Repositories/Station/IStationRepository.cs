using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Station;

public interface IStationRepository
{
    /// <summary>
    /// 停車場のみを取得する
    /// </summary>
    /// <returns>停車場のリスト</returns>
    Task<List<Models.Station>> GetWhereIsStation();

    /// <summary>
    /// IDで停車場を取得する
    /// </summary>
    /// <param name="id">停車場のID</param>
    /// <returns>該当する停車場、存在しない場合はnull</returns>
    Task<Models.Station?> GetStationById(string id);

    /// <summary>
    /// 名前で停車場を取得する
    /// </summary>
    /// <param name="name">停車場の名前</param>
    /// <returns>該当する停車場、存在しない場合はnull</returns>
    Task<Models.Station?> GetStationByName(string name);

    /// <summary>
    /// 複数のIDで停車場を取得する
    /// </summary>
    /// <param name="ids">停車場のIDのリスト</param>
    /// <returns>該当する停車場のリスト</returns>
    Task<List<Models.Station>> GetStationByIds(IEnumerable<string> ids);

    /// <summary>
    /// すべてのタイマー状態を取得する
    /// </summary>
    /// <returns>タイマー状態のリスト</returns>
    Task<List<StationTimerState>> GetAllTimerStates();

    /// <summary>
    /// 複数の停車場IDに対応するタイマー状態を取得する
    /// </summary>
    /// <param name="stationIds">停車場のIDのリスト</param>
    /// <returns>該当するタイマー状態のリスト</returns>
    Task<List<StationTimerState>> GetTimerStatesByStationIds(IEnumerable<string> stationIds);
}
