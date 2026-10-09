using Microsoft.AspNetCore.SignalR;

namespace Traincrew_MultiATS_Server.Hubs;

/// <summary>
/// 接続ごとに「メソッドごとの最新1件」だけを送る配信。1接続の詰まりが呼び出し側に波及しない。
/// 順序が必要なメソッド同士は必ず同じ経路に揃えること(現在、定時送信はすべてこの経路)。
/// </summary>
/// <remarks>
/// 戻り値のTaskは全接続のmailboxに置いた時点で完了する。送信の完了ではない。
/// 実際のシリアライズは各接続のpump側で遅延実行されるので、渡したargs(リスト・DTO)は渡した後に変更・再利用しないこと。
/// キャッシュ由来のオブジェクトを渡すなら、それが不変であること。
/// 全状態のスナップショットで古い分を捨ててよい配信だけを、呼び出し側(スケジューラ)が選んで使う。
/// メソッドはnameof(IXxxClientContract.ReceiveXxx)で指定する。名前の存在はコンパイル時に確認できるが、引数の型は確認されない。
/// 引数の型安全を保つ型付きプロキシは、実装コストに見合わないので作らない。
/// </remarks>
public interface ILatestOnlySender<THub> where THub : Hub
{
    Task SendAllLatestAsync(string method, params object?[] args);
}

/// <summary>
/// HubLifetimeManagerをLatestOnlyHubLifetimeManagerとして扱い、SendAllLatestAsyncへ渡す。
/// </summary>
public class LatestOnlySender<THub>(HubLifetimeManager<THub> hubLifetimeManager) : ILatestOnlySender<THub>
    where THub : Hub
{
    public Task SendAllLatestAsync(string method, params object?[] args)
    {
        // 差し替え漏れ(Program.csの登録忘れ)のとき、黙って既存のSendAllAsyncへ落とすと、
        // 詰まりが全員に波及する元の問題が直らないまま気付けなくなる。例外にして最初の配信で発覚させる
        if (hubLifetimeManager is not LatestOnlyHubLifetimeManager<THub> manager)
        {
            throw new InvalidOperationException(
                $"HubLifetimeManager<{typeof(THub).Name}> が {nameof(LatestOnlyHubLifetimeManager<THub>)} に差し替えられていません");
        }

        // 全接続のmailboxに置いて即returnする
        return manager.SendAllLatestAsync(method, args);
    }
}
