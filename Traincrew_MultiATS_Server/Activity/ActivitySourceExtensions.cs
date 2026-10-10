using System.Diagnostics;

namespace Traincrew_MultiATS_Server.Activity;

public static class ActivitySourceExtensions
{
    /// <summary>
    /// 呼び出し元メソッド名ではなく、指定した名前でActivityを開始する。
    /// ActivitySource.StartActivityの名前引数は[CallerMemberName]付きなので、
    /// 「Broadcast.Send.Tid」のような階層名を付けたいときはこちらを使う。
    /// </summary>
    public static System.Diagnostics.Activity? StartNamedActivity(this ActivitySource source, string name)
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        return source.StartActivity(name);
    }
}
