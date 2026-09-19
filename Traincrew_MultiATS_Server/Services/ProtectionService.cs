using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Protection;

namespace Traincrew_MultiATS_Server.Services;

public interface IProtectionService
{
    Task<bool> EvaluateAndUpdateBougo(string trainNumber, List<TrackCircuit> trackCircuits, bool clientBougoState);
    Task<List<ProtectionRadioData>> GetProtectionRadioStates();
    Task AddProtectionZoneState(ProtectionRadioData data);
    Task UpdateProtectionZoneState(ProtectionRadioData data);
    Task DeleteProtectionZoneState(ulong id);
}

public class ProtectionService(
    IProtectionRepository protectionRepository,
    IGeneralRepository generalRepository) : IProtectionService
{
    /// <summary>
    /// 防護無線の受報判定と発報状態の更新を、同一tickの1回の読みで行う。
    /// </summary>
    /// <remarks>
    /// キャッシュは意図的に使わない。古い値で受報漏れ・発報漏れが起きるのが最も危険なため、
    /// 判定は必ずそのtickで読んだ protection_zone_state の全件に対して行う。
    /// 省略するのは「自分の行が無いときに解除(DELETE)を打たない」方向だけで、
    /// これは安全側であり、かつ次のtickの読みで自己修復する。
    /// 逆に発報(EnableProtection)は、既に同じゾーン集合が入っていても必ず打つ
    /// (読みと書きの間に司令卓が行を消すと発報漏れになるため)。
    ///
    /// 受報判定は書き込みの前のスナップショットで行うため、自分自身の発報・移動が
    /// 自分の受報に反映されるのは次のtickになる。これは統合前と同じ挙動である。
    /// </remarks>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="trackCircuits">その列車の在線軌道回路</param>
    /// <param name="clientBougoState">クライアントが防護無線を発報しているか</param>
    /// <returns>その列車が防護無線を受報しているか</returns>
    public async Task<bool> EvaluateAndUpdateBougo(
        string trainNumber, List<TrackCircuit> trackCircuits, bool clientBougoState)
    {
        // 全件取得。通常0行、発報中でも数行の極小テーブルであり、
        // パラメータが無いので文面が固定されauto-prepareが確実に効く。
        var states = await protectionRepository.GetAll();

        var bougoState = IsProtectionEnabledForTrackCircuits(states, trackCircuits);

        if (clientBougoState)
        {
            // 発報は毎回打つ。ゾーンの追加・削除の差分計算はリポジトリ側の責務。
            // なお在線が空のときは空のゾーンリストを渡すことになり、結果その列車の行は
            // 全削除される(実質解除)。統合前と同じ挙動。
            await protectionRepository.Enable(
                trainNumber, trackCircuits.Select(tc => tc.ProtectionZone).ToList());
        }
        else if (states.Any(state => state.TrainNumber == trainNumber))
        {
            // 自分の行が無いなら空振りのDELETEを打たない(毎tick全クライアント分発行されていた)
            await protectionRepository.Disable(trainNumber);
        }

        return bougoState;
    }

    /// <summary>
    /// 在線軌道回路の防護範囲(在線ゾーンの最小-1 〜 最大+1)に発報中のゾーンがあるかを判定する。
    /// </summary>
    /// <remarks>
    /// 最小・最大は在線軌道回路の集合全体から取る。ゾーン境界にまたがって停止した場合は
    /// 判定範囲がその分広がる。
    /// </remarks>
    private static bool IsProtectionEnabledForTrackCircuits(
        List<ProtectionZoneState> states, List<TrackCircuit> trackCircuits)
    {
        if (trackCircuits.Count == 0)
        {
            // 在線が取得できていない場合は判定不能。Min()/Max()は空リストで例外になる。
            return false;
        }

        // 防護範囲の最大、最小を求め、それの+1、-1を求める
        var protectionZone = trackCircuits.Select(tc => tc.ProtectionZone).ToList();
        var minProtectionZone = protectionZone.Min() - 1;
        var maxProtectionZone = protectionZone.Max() + 1;
        // その防護範囲で防護無線が発報されているか確認
        return states.Any(state =>
            minProtectionZone <= state.ProtectionZone && state.ProtectionZone <= maxProtectionZone);
    }

    // ProtectionZoneStateの取得
    public async Task<List<ProtectionRadioData>> GetProtectionRadioStates()
    {
        var entities = await protectionRepository.GetAll();
        return entities
            .Select(entity => new ProtectionRadioData
            {
                Id = entity.id,
                TrainNumber = entity.TrainNumber,
                ProtectionZone = entity.ProtectionZone
            })
            .ToList();
    }

    // ProtectionZoneStateの追加
    public async Task AddProtectionZoneState(ProtectionRadioData data)
    {
        await generalRepository.Add(new ProtectionZoneState
        {
            TrainNumber = data.TrainNumber,
            ProtectionZone = data.ProtectionZone
        });
    }

    // ProtectionZoneStateの更新
    public async Task UpdateProtectionZoneState(ProtectionRadioData data)
    {
        await generalRepository.Save(new ProtectionZoneState
        {
            id = data.Id,
            TrainNumber = data.TrainNumber,
            ProtectionZone = data.ProtectionZone
        });
    }

    // ProtectionZoneStateの削除
    public async Task DeleteProtectionZoneState(ulong id)
    {
        await protectionRepository.DeleteById(id);
    }
}
