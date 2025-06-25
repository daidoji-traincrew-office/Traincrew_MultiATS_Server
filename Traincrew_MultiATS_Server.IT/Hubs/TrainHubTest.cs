using Moq;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.IT.Hubs;

[Collection("WebApplication")]
public class TrainHubTest(WebApplicationFixture factory)
{
    [Fact(DisplayName = "他に列車がおらず、新規登録するとき")]
    public async Task SendData_ATS_NewTrain_RegistersSuccessfully()
    {
        const string trainNumber = "1160";
        // Arrange
        var mockClient = new Mock<ITrainClientContract>();
        var (connection, hub) = factory.CreateTrainHub(mockClient.Object);

        var atsData = new AtsToServerData
        {
            DiaName = trainNumber,
            OnTrackList = [new (){Name = "TH76_5LDT"}],
            CarStates = [new (){
                Ampare = 0,
                BC_Press = 0,
                CarModel = "5320",
                DoorClose = true,
                HasConductorCab = true,
                HasDriverCab = true,
                HasMotor = true,
                HasPantograph = true,
            }],
            BougoState = false,
            IsTherePreviousTrainIgnore = false
        };

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act
                var result = await hub.SendData_ATS(atsData);

                // Assert
                Assert.NotNull(result);
                Assert.False(result.IsTherePreviousTrain);
                Assert.False(result.IsOnPreviousTrain);
            }
            finally
            {
                await DeleteTrainsAsync([trainNumber]);
            }
        }
    }

    [Fact(DisplayName = "運転士のいない列車がいて、引き継ぐ場合")]
    public async Task SendData_ATS_TakeOverTrainWithoutDriver()
    {
        const string trainNumber = "1162";
        const string oldTrainNumber = "1163";
        // Arrange
        var mockClient = new Mock<ITrainClientContract>();
        var (connection, hub) = factory.CreateTrainHub(mockClient.Object);
        var (commanderConnection, commanderHub) = factory.CreateCommanderTableHub();
        var db = factory.CreateTrainRepository();

        // 事前に運転士のいない列車を登録
        var trainState = new TrainState
        {
            TrainNumber = oldTrainNumber,
            DiaNumber = 62,
            DriverId = null,
            FromStationId = "TH00",
            ToStationId = "TH00",
            Delay = 0
        };
        await db.Create(trainState);
        // 軌道回路を埋める
        await using (commanderConnection) 
        {
            await commanderConnection.StartAsync(TestContext.Current.CancellationToken);
            await commanderHub.SendTrackCircuitData(new () 
            {
                Name = "TH76_5LDT",
                Last = oldTrainNumber,
                On = true,
                Lock = false
            });
        }

        var atsData = new AtsToServerData
        {
            DiaName = trainNumber,
            OnTrackList = [new (){Name = "TH76_5LDT"}],
            CarStates = [new (){
                Ampare = 0,
                BC_Press = 0,
                CarModel = "5320",
                DoorClose = true,
                HasConductorCab = true,
                HasDriverCab = true,
                HasMotor = true,
                HasPantograph = true,
            }],
            BougoState = false,
            IsTherePreviousTrainIgnore = false
        };

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act
                var result = await hub.SendData_ATS(atsData);

                // Assert
                Assert.NotNull(result);
                Assert.False(result.IsTherePreviousTrain);
                Assert.False(result.IsOnPreviousTrain);

                // DB上でDriverIdがセットされていることを確認
                var updatedTrain = (await db.GetByTrainNumbers([trainNumber])).FirstOrDefault();
                Assert.NotNull(updatedTrain);
                Assert.NotNull(updatedTrain.DriverId);
            }
            finally
            {
                await DeleteTrainsAsync([trainNumber, oldTrainNumber]);
            }
        }
    }

    [Fact(DisplayName = "他人が運転している列車がいて、交代前応答が来る場合")]
    public async Task SendData_ATS_AnotherDriverExists_RespondsWithIsTherePreviousTrain()
    {
        const string trainNumber = "1164";
        const string otherDriverTrainNumber = "1165";
        // Arrange
        var mockClient = new Mock<ITrainClientContract>();
        var (connection, hub) = factory.CreateTrainHub(mockClient.Object);
        var db = factory.CreateTrainRepository();

        // 事前に他人が運転している列車を登録
        var trainState = new TrainState
        {
            TrainNumber = otherDriverTrainNumber,
            DiaNumber = 64,
            DriverId = 9999UL, // 他人のDriverId
            FromStationId = "TH00",
            ToStationId = "TH00",
            Delay = 0
        };
        await db.Create(trainState);

        var atsData = new AtsToServerData
        {
            DiaName = trainNumber,
            OnTrackList = [new (){Name = "TH76_5LDT"}],
            CarStates = [new (){
                Ampare = 0,
                BC_Press = 0,
                CarModel = "5320",
                DoorClose = true,
                HasConductorCab = true,
                HasDriverCab = true,
                HasMotor = true,
                HasPantograph = true,
            }],
            BougoState = false,
            IsTherePreviousTrainIgnore = false
        };

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act
                var result = await hub.SendData_ATS(atsData);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.IsTherePreviousTrain); // 交代前応答
            }
            finally
            {
                await DeleteTrainsAsync([trainNumber, otherDriverTrainNumber]);
            }
        }
    }

    [Fact(DisplayName = "自分が運転している列車がいて、列番を変更しない場合")]
    public async Task SendData_ATS_SameDriver_SameTrainNumber_UpdatesSuccessfully()
    {
        const string trainNumber = "1266";
        // Arrange
        var mockClient = new Mock<ITrainClientContract>();
        var (connection, hub) = factory.CreateTrainHub(mockClient.Object);
        var db = factory.CreateTrainRepository();

        // 事前に自分が運転している列車を登録
        var myDriverId = WebApplicationFixture.DriverId;
        var trainState = new TrainState
        {
            TrainNumber = trainNumber,
            DiaNumber = 66,
            DriverId = myDriverId,
            FromStationId = "TH00",
            ToStationId = "TH00",
            Delay = 0
        };
        await db.Create(trainState);

        var atsData = new AtsToServerData
        {
            DiaName = trainNumber,
            OnTrackList = [new (){Name = "TH76_5LDT"}],
            CarStates = [new (){
                Ampare = 0,
                BC_Press = 0,
                CarModel = "5320",
                DoorClose = true,
                HasConductorCab = true,
                HasDriverCab = true,
                HasMotor = true,
                HasPantograph = true,
            }],
            BougoState = false,
            IsTherePreviousTrainIgnore = false
        };

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act
                var result = await hub.SendData_ATS(atsData);

                // Assert
                Assert.NotNull(result);
                Assert.False(result.IsTherePreviousTrain);
                Assert.False(result.IsOnPreviousTrain);
                // DB上でDriverIdが変わらず、列番も変わらないこと
                var updatedTrain = (await db.GetByTrainNumbers([trainNumber])).FirstOrDefault();
                Assert.NotNull(updatedTrain);
                Assert.Equal(myDriverId, updatedTrain.DriverId);
            }
            finally
            {
                await DeleteTrainsAsync([trainNumber]);
            }
        }
    }

    [Fact(DisplayName = "自分が運転している列車がいて、列番を変更する場合")]
    public async Task SendData_ATS_SameDriver_ChangeTrainNumber_UpdatesTrainNumber()
    {
        const string trainNumber = "1268";
        // Arrange
        var mockClient = new Mock<ITrainClientContract>();
        var (connection, hub) = factory.CreateTrainHub(mockClient.Object);
        var db = factory.CreateTrainRepository();

        // 事前に自分が運転している列車を登録
        var myDriverId = WebApplicationFixture.DriverId;
        var trainState = new TrainState
        {
            TrainNumber = "1267",
            DiaNumber = 68,
            DriverId = myDriverId,
            FromStationId = "TH00",
            ToStationId = "TH00",
            Delay = 0
        };
        await db.Create(trainState);

        var atsData = new AtsToServerData
        {
            DiaName = trainNumber, // 列番を変更
            OnTrackList = [new (){Name = "TH76_5LDT"}],
            CarStates = [new (){
                Ampare = 0,
                BC_Press = 0,
                CarModel = "5320",
                DoorClose = true,
                HasConductorCab = true,
                HasDriverCab = true,
                HasMotor = true,
            }],
            BougoState = false,
            IsTherePreviousTrainIgnore = false
        };

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act
                var result = await hub.SendData_ATS(atsData);

                // Assert
                Assert.NotNull(result);
                Assert.False(result.IsTherePreviousTrain);
                Assert.False(result.IsOnPreviousTrain);
                // DB上でDriverIdが変わらず、列番が変更されていること
                var updatedTrain = (await db.GetByTrainNumbers([trainNumber])).FirstOrDefault();
                Assert.NotNull(updatedTrain);
                Assert.Equal(myDriverId, updatedTrain.DriverId);
            }
            finally
            {
                await DeleteTrainsAsync([trainNumber]);
            }
        }
    }
    
    [Fact(DisplayName = "折り返し変更(運行番号変更)を行う場合")]
    public async Task SendData_ATS_ChangeDiaNumber_UpdatesTrainNumber()
    {
        // 70運行->72運行に変更する例
        const string oldTrainNumber = "1269";
        const string trainNumber = "1272";
        // Arrange
        var mockClient = new Mock<ITrainClientContract>();
        var (connection, hub) = factory.CreateTrainHub(mockClient.Object);
        var db = factory.CreateTrainRepository();

        // 事前に自分が運転していた列車を登録
        var myDriverId = WebApplicationFixture.DriverId;
        var trainState = new TrainState
        {
            TrainNumber = oldTrainNumber,
            DiaNumber = 70,
            DriverId = null,
            FromStationId = "TH00",
            ToStationId = "TH00",
            Delay = 0
        };
        await db.Create(trainState);

        var atsData = new AtsToServerData
        {
            DiaName = trainNumber, // 列番を変更
            OnTrackList = [new (){Name = "TH76_5LDT"}],
            CarStates = [new (){
                Ampare = 0,
                BC_Press = 0,
                CarModel = "5320",
                DoorClose = true,
                HasConductorCab = true,
                HasDriverCab = true,
                HasMotor = true,
            }],
            BougoState = false,
            IsTherePreviousTrainIgnore = false
        };

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act
                var result = await hub.SendData_ATS(atsData);

                // Assert
                Assert.NotNull(result);
                Assert.False(result.IsTherePreviousTrain);
                Assert.False(result.IsOnPreviousTrain);
                // DB上でDriverIdが変わらず、列番が変更されていること
                var updatedTrain = (await db.GetByTrainNumbers([trainNumber])).FirstOrDefault();
                Assert.NotNull(updatedTrain);
                Assert.Equal(myDriverId, updatedTrain.DriverId);
                Assert.Equal(72, updatedTrain.DiaNumber); // 運行番号が変更されていること
                Assert.Equal(trainNumber, updatedTrain.TrainNumber);
            }
            finally
            {
                await DeleteTrainsAsync([trainNumber]);
            }
        }
    }

    private async Task DeleteTrainsAsync(List<string> trainNumbers)
    {
        var (connection, hub) = factory.CreateCommanderTableHub();
        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);
            foreach (var trainNumber in trainNumbers)
            {
                await hub.DeleteTrain(trainNumber);
            }
        }
    }
}