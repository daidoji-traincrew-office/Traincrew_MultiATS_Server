using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// DirectionRouteに関するサービスクラス
/// </summary>
public class DirectionRouteService(IDirectionRouteRepository directionRouteRepository)
{
    /// <summary>
    /// DirectionRouteをDirectionDataに変換する
    /// </summary>
    /// <param name="direction">DirectionRouteオブジェクト</param>
    /// <returns>DirectionDataオブジェクト</returns>
    private static DirectionData ToDirectionData(DirectionRoute direction)
    {
        if (direction.DirectionRouteState == null)
        {
            throw new ArgumentException("Invalid direction state");
        }

        var state = direction.DirectionRouteState.isLr switch
        {
            LR.Left => LCR.Left,
            LR.Right => LCR.Right,
            _ => LCR.Center
        };

        return new()
        {
            Name = direction.Name,
            State = state
        };
    }

    /// <summary>
    /// 全てのDirectionRouteを取得し、DirectionDataに変換して返す
    /// </summary>
    /// <returns>DirectionDataのリスト</returns>
    public async Task<List<DirectionData>> GetAllDirectionData()
    {
        var directionRoutes = await directionRouteRepository.GetAllWithState();
        return directionRoutes.Select(ToDirectionData).ToList();
    }
}
