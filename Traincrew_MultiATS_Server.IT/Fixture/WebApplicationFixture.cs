using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Crew;
using TypedSignalR.Client;

namespace Traincrew_MultiATS_Server.IT.Fixture;

public class WebApplicationFixture : IAsyncLifetime
{
    private const string TrainHubPath = "/hub/train";
    private const string TIDHubPath = "/hub/TID";
    private const string InterlockingHubPath = "/hub/interlocking";
    private const string CommanderTableHubPath = "/hub/commander_table";

    private WebApplicationFactory<Program> factory = new();

    public WebApplicationFixture()
    {
        // Shift-JISを使用するために、CodePagesEncodingProviderを登録
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async ValueTask InitializeAsync()
    {
        await StartScheduledTask();
    }


    public async ValueTask DisposeAsync()
    {
        await factory.DisposeAsync();
    }

    private async Task StartScheduledTask()
    {
        var mockClientContract = new Mock<ICommanderTableClientContract>();
        var (connection, hub) = CreateCommanderTableHub(mockClientContract.Object);

        await using (connection)
        {
            await connection.StartAsync();
            await hub.SetServerMode(ServerMode.Private).WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    public static ulong DriverId => 0UL; // テスト用の固定DriverId

    /// <summary>
    /// ITrainHubContractとITrainClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnection, ITrainHubContract</returns>
    public (HubConnection, ITrainHubContract) CreateTrainHub(ITrainClientContract? receiver = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, TrainHubPath),
                o => { o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        var hubContract = connection.CreateHubProxy<ITrainHubContract>();
        if (receiver != null)
        {
            connection.Register(receiver);
        }

        return (connection, hubContract);
    }

    /// <summary>
    /// テスト用にDIにあるものを取得する
    /// </summary>
    public T Create<T>() where T : notnull
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// ITIDHubContractとITIDClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnectionとITIDHubContract</returns>
    public (HubConnection, ITIDHubContract) CreateTIDHub(ITIDClientContract? receiver = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, TIDHubPath),
                o => { o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        var hubContract = connection.CreateHubProxy<ITIDHubContract>();
        if (receiver != null)
        {
            connection.Register(receiver);
        }

        return (connection, hubContract);
    }

    /// <summary>
    /// IInterlockingHubContractとIInterlockingClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnectionとIInterlockingHubContract</returns>
    public (HubConnection, IInterlockingHubContract) CreateInterlockingHub(IInterlockingClientContract? receiver = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, InterlockingHubPath),
                o => { o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        var hubContract = connection.CreateHubProxy<IInterlockingHubContract>();
        if (receiver != null)
        {
            connection.Register(receiver);
        }

        return (connection, hubContract);
    }

    /// <summary>
    /// ICommanderTableHubContractとICommanderTableClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnectionとICommanderTableHubContract</returns>
    public (HubConnection, ICommanderTableHubContract) CreateCommanderTableHub(
        ICommanderTableClientContract? receiver = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, CommanderTableHubPath),
                o => { o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        var hubContract = connection.CreateHubProxy<ICommanderTableHubContract>();
        if (receiver != null)
        {
            connection.Register(receiver);
        }

        return (connection, hubContract);
    }
}