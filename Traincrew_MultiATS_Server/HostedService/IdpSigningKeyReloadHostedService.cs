using Traincrew_MultiATS_Server.Authentication;

namespace Traincrew_MultiATS_Server.HostedService;

/// <summary>
/// 出勤台 IdP の署名公開鍵を、起動時と定期的に読み直す。
/// 次の鍵は切り替えの 90 日前に置かれるので、1 時間ごとの読み直しで十分に間に合う。
/// 鍵が 1 つも無い間は、見つかるまで短い間隔で再試行する。
/// </summary>
public class IdpSigningKeyReloadHostedService(
    IdpSigningKeyStore store,
    TimeProvider timeProvider,
    ILogger<IdpSigningKeyReloadHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan ReloadInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan RetryIntervalWhileEmpty = TimeSpan.FromMinutes(1);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // リクエストを受け付ける前に、起動時の鍵を読み込んでおく
        store.Reload();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = store.KeyIds.Count == 0 ? RetryIntervalWhileEmpty : ReloadInterval;
            try
            {
                await Task.Delay(interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                store.Reload();
            }
            catch (System.Exception e)
            {
                // 読み直しの失敗で Crew 全体を止めない(BackgroundService の未処理例外は既定でホストを止める)。
                // 前回の鍵を使い続け、次の読み直しで再試行する
                logger.LogError(e, "出勤台 IdP の署名公開鍵の読み直しに失敗しました。前回の鍵を使い続けます。");
            }
        }
    }
}
