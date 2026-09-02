using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service.TestHelpers;

/// <summary>
/// ServerServiceのテスト用ヘルパークラス
/// モック可能なメソッドを提供
/// </summary>
public class TestServerService() : ServerService(null!, null!, null!)
{
    private Func<Task<int>>? getTimeOffsetAsyncFunc;

    /// <summary>
    /// GetTimeOffsetAsyncの動作を設定
    /// </summary>
    public void SetupGetTimeOffsetAsync(Func<Task<int>> func)
    {
        getTimeOffsetAsyncFunc = func;
    }

    public override async Task<int> GetTimeOffsetAsync()
    {
        if (getTimeOffsetAsyncFunc != null)
        {
            return await getTimeOffsetAsyncFunc();
        }
        return 0;
    }
}
