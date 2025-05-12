using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Traincrew_MultiATS_Server.Common.Contract;
using TypedSignalR.Client;

namespace Traincrew_MultiATS_Server.IT.Fixture;

public class WebApplicationFixture : WebApplicationFactory<Program>
{
    private const string TrainHubPath = "/hub/train";
    private const string TIDHubPath = "/hub/TID";
    private const string InterlockingHubPath = "/hub/interlocking";
    private const string CommanderTableHubPath = "/hub/commander_table";

    /// <summary>
    /// ITrainHubContractとITrainClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnectionとITrainHubContract</returns>
    public (HubConnection, ITrainHubContract) CreateTrainHub(ITrainClientContract? receiver = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(Server.BaseAddress + TrainHubPath)
            .Build();

        var hubContract = connection.CreateHubProxy<ITrainHubContract>();
        if (receiver != null)
        {
            connection.Register(receiver);
        }

        return (connection, hubContract);
    }

    /// <summary>
    /// ITIDHubContractとITIDClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnectionとITIDHubContract</returns>
    public (HubConnection, ITIDHubContract) CreateTIDHub(ITIDClientContract receiver)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(Server.BaseAddress + TIDHubPath)
            .Build();

        var hubContract = connection.CreateHubProxy<ITIDHubContract>();
        connection.Register(receiver);

        return (connection, hubContract);
    }

    /// <summary>
    /// IInterlockingHubContractとIInterlockingClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnectionとIInterlockingHubContract</returns>
    public (HubConnection, IInterlockingHubContract) CreateInterlockingHub(IInterlockingClientContract receiver)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(Server.BaseAddress + InterlockingHubPath)
            .Build();

        var hubContract = connection.CreateHubProxy<IInterlockingHubContract>();
        connection.Register(receiver);

        return (connection, hubContract);
    }

    /// <summary>
    /// ICommanderTableHubContractとICommanderTableClientContract用のHubConnectionを生成し、両方を返します。
    /// </summary>
    /// <param name="receiver">クライアント側のコントラクトを実装したインスタンス</param>
    /// <returns>HubConnectionとICommanderTableHubContract</returns>
    public (HubConnection, ICommanderTableHubContract) CreateCommanderTableHub(ICommanderTableClientContract receiver)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(Server.BaseAddress + CommanderTableHubPath)
            .Build();

        var hubContract = connection.CreateHubProxy<ICommanderTableHubContract>();
        connection.Register(receiver);

        return (connection, hubContract);
    }
}