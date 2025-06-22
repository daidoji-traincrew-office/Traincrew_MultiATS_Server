using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Crew;
using Traincrew_MultiATS_Server.Repositories.Train;
using TypedSignalR.Client;

namespace Traincrew_MultiATS_Server.IT.Fixture;

public class WebApplicationFixture
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
    /// テスト用にITrainRepositoryを取得する
    /// </summary>
    public ITrainRepository CreateTrainRepository()
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITrainRepository>();
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