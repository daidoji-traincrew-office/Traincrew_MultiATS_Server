using System.Timers;
using Timer = System.Timers.Timer;
namespace Traincrew_MultiATS_Server.Services;
class TimedEventPublisher
{
    private Timer _timer;
    private EventHandler? _onIntervalElapsed; // バックフィールド

    // イベントの定義
    public event EventHandler OnIntervalElapsed
    {
        add
        {
            _onIntervalElapsed += value;
            // Console.WriteLine("イベントが購読されました。購読者数: " + _onIntervalElapsed?.GetInvocationList().Length);

            if (!_timer.Enabled)
            {
                _timer.Start();
                // Console.WriteLine("タイマーを開始しました。");
            }
        }
        remove
        {
            _onIntervalElapsed -= value;
            Console.WriteLine("イベントが解除されました。購読者数: " + (_onIntervalElapsed?.GetInvocationList().Length ?? 0));

            if (_onIntervalElapsed == null || _onIntervalElapsed.GetInvocationList().Length == 0)
            {
                _timer.Stop();
                // Console.WriteLine("タイマーを停止しました。");
            }
        }
    }

    public  TimedEventPublisher(double interval)
    {
        // タイマーの初期化
        _timer = new Timer(interval)
        {
            AutoReset = true // 繰り返し動作
        };
        _timer.Elapsed += HandleTimerElapsed!;
    }

    // タイマーが経過した際にイベントを発行
    private void HandleTimerElapsed(object sender, ElapsedEventArgs e)
    {
        _onIntervalElapsed?.Invoke(this, EventArgs.Empty);
    }
}