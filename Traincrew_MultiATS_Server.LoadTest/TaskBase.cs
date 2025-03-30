using Microsoft.AspNetCore.SignalR.Client;

namespace Traincrew_MultiATS_Server.LoadTest;

public abstract class TaskBase
{
    protected abstract int Delay { get; }
    protected abstract string Path { get; }
    protected abstract string Method { get; }
    private readonly List<CancellationTokenSource> _cancellationTokens = [];
    
    // タスクを実行するメソッド
    private async Task ExecuteTask(CancellationToken token)
    {
        try
        {
            // Todo: ServerAddress.csから取得できるようにしたほうが良いかな
            var connection = new HubConnectionBuilder()
            .WithUrl($"https://localhost:7232/hub/{Path}" )
            .WithAutomaticReconnect()
            .Build();
            await connection.StartAsync(token);
            while (true)
            {
                var delay = Task.Delay(Delay, token);
                await connection.InvokeAsync(Method, token);
                await delay;
            }
        }
        catch (TaskCanceledException)
        {
            // タスクがキャンセルされた場合の処理
        }
        catch (Exception ex)
        {
            // その他の例外処理
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    // 現在のカウントを返すメソッド
    public int GetCurrentCount()
    {
        return _cancellationTokens.Count;
    }

    // 現在の値を増やし、新しいタスクを実行するメソッド
    public void IncrementAndExecute()
    {
        var source = new CancellationTokenSource();
        Task.Run(() => ExecuteTask(source.Token), CancellationToken.None);
        _cancellationTokens.Add(source);
    }

    // 3. 現在の値を減らし、現在実行中のタスクを1つキャンセルするメソッド
    public void DecrementAndCancel()
    {
        if (GetCurrentCount() <= 0) return;
        _cancellationTokens.Last().Cancel();
        _cancellationTokens.RemoveAt(_cancellationTokens.Count - 1);
    }
}

public class ATS : TaskBase
{
    protected override int Delay => 100;
    protected override string Path => "train";
    protected override string Method => "SendData_ATS";
}

public class Signal : TaskBase
{
    protected override int Delay => 100;
    protected override string Path => "interlocking";
    protected override string Method => "SendData_Interlocking";
}

public class TID : TaskBase
{
    protected override int Delay => 333;
    protected override string Path => "TID";
    protected override string Method => "SendData_TID";
}