using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service.TestHelpers;

/// <summary>
/// TrackCircuitServiceのテスト用ヘルパークラス
/// モック可能なメソッドを提供
/// </summary>
public class TestTrackCircuitService() : TrackCircuitService(null!, null!)
{
    private Func<List<string>, Task<List<TrackCircuit>>>? getTrackCircuitsByNamesFunc;

    /// <summary>
    /// GetTrackCircuitsByNamesの動作を設定
    /// </summary>
    public void SetupGetTrackCircuitsByNames(Func<List<string>, Task<List<TrackCircuit>>> func)
    {
        getTrackCircuitsByNamesFunc = func;
    }

    public override async Task<List<TrackCircuit>> GetTrackCircuitsByNames(List<string> trackCircuitNames)
    {
        if (getTrackCircuitsByNamesFunc != null)
        {
            return await getTrackCircuitsByNamesFunc(trackCircuitNames);
        }
        return [];
    }
}
