using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public interface ITIDService
{
    Task<ConstantDataToTID> CreateTidData();
    ConstantDataToTID BuildTidData(CommonReads commonReads);
}

public class TIDService(ICommonReadsBuilder commonReadsBuilder) : ITIDService
{
    public async Task<ConstantDataToTID> CreateTidData()
    {
        var commonReads = await commonReadsBuilder.BuildAsync();
        return BuildTidData(commonReads);
    }

    /// <summary>
    /// <see cref="CommonReads"/> からTID配信用データを組み立てる。
    /// 追加読み取りが無いため同期メソッドでよい。
    /// </summary>
    public ConstantDataToTID BuildTidData(CommonReads commonReads)
    {
        return new()
        {
            TrackCircuitDatas = commonReads.TrackCircuits,
            SwitchDatas = commonReads.Switches,
            DirectionDatas = commonReads.Directions,
            TrainStateDatas = commonReads.TrainStates,
            TimeOffset = commonReads.TimeOffset
        };
    }
}
