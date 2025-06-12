using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Services;

public class TrainService(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    OperationNotificationService operationNotificationService,
    ProtectionService protectionService,
    RouteService routeService)
{
    public async Task<ServerToATSData> CreateAtsData(long? clientDriverId, AtsToServerData clientData)
    {
        // ‹O“¹‰ñ˜Hî•ñ‚ÌXV
        List<TrackCircuit> oldTrackCircuitList =
            await trackCircuitService.GetTrackCircuitsByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> oldTrackCircuitDataList =
            oldTrackCircuitList.Select(TrackCircuitService.ToTrackCircuitData).ToList();
        /// <summary>
        /// V‹K“o˜^‹O“¹‰ñ˜H
        /// </summary>
        List<TrackCircuitData> incrementalTrackCircuitDataList =
            clientData.OnTrackList.Except(oldTrackCircuitDataList).ToList();
        /// <summary>
        /// İüI—¹‹O“¹‰ñ˜H    
        /// </summary>
        List<TrackCircuitData> decrementalTrackCircuitDataList =
            oldTrackCircuitDataList.Except(clientData.OnTrackList).ToList();

        // ‹O“¹‰ñ˜H‚ğæ“¾‚µ‚æ‚¤‚Æ‚·‚é
        var trackCircuitList = await trackCircuitService.GetTrackCircuitsByNames(
            clientData.OnTrackList.Select(tcd => tcd.Name).ToList());
        // Todo: •¶š‰»‚¯‚Ö‚Ì‘Î‰‚ª‚Å‚«‚½‚çˆÈ‰º‚Ìˆ—‚Í‚¢‚ç‚È‚¢
        // æ“¾‚Å‚«‚È‚¢‹O“¹‰ñ˜H‚ª‚ ‚éê‡Aˆê’U‘O‰ñ‚Ìƒf[ƒ^‚ğg‚¤
        if (trackCircuitList.Count != clientData.OnTrackList.Count)
        {
            trackCircuitList = oldTrackCircuitList;
        }



        var ClientTrainNumber = clientData.DiaName;
        // —ñÔ“o˜^î•ñæ“¾
        var TrainStates = new List<TrainState>();
        // ‰^”Ô‚ª“¯‚¶—ñÔ‚Ìî•ñ‚ğæ“¾‚·‚é
        var TrainState = TrainStates.FirstOrDefault(ts => IsTrainNumberEqual(ts.TrainNumber, ClientTrainNumber));

        ServerToATSData serverData = new ServerToATSData();


        // ™î•ñ‚ÍŠ„‚Æí‚É‘—‚é‚½‚ß‹¤’Ê‚Å‰‰Z‚·‚é   

        // İü‚µ‚Ä‚¢‚é‹O“¹‰ñ˜Hã‚Å–hŒì–³ü‚ª”­•ñ‚³‚ê‚Ä‚¢‚é‚©Šm”F
        serverData.BougoState = await protectionService.IsProtectionEnabledForTrackCircuits(trackCircuitList);
        // –hŒì–³ü‚ğ”­•ñ‚µ‚Ä‚¢‚éê‡‚ÌDBXV
        if (clientData.BougoState)
        {
            await protectionService.EnableProtectionByTrackCircuits(clientData.DiaName, trackCircuitList);
        }
        else
        {
            await protectionService.DisableProtection(clientData.DiaName);
        }

        // ‰^“]’mŠí‚Ì•\¦
        serverData.OperationNotificationData = await operationNotificationService
            .GetOperationNotificationDataByTrackCircuitIds(trackCircuitList.Select(tc => tc.Id).ToList());

        // M†Œ»¦‚ÌŒvZ
        // ã‚è‚©‰º‚è‚©”»’f(‹ô”‚È‚çã‚èAŠï”‚È‚ç‰º‚è)
        var lastDiaNumber = clientData.DiaName.Last(char.IsDigit) - '0';
        var isUp = lastDiaNumber % 2 == 0;
        // ŠY“–‹O“¹‰ñ˜H‚ÌM†‹@‚ğ‘Sæ“¾
        var signalNames = await signalService
            .GetSignalNamesByTrackCircuits(trackCircuitList, isUp);
        // Œ»¦ŒvZ
        // Todo: 1‚Âæ‚ÌM†‹@‚Ü‚Å‚ÍÅ’áŒÀŒvZ‚·‚é
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        serverData.NextSignalData = signalIndications.Select(pair => new SignalData
        {
            Name = pair.Key,
            phase = pair.Value
        }).ToList();
        serverData.RouteData = await routeService.GetActiveRoutes();

        // 1.“¯ˆê—ñ”Ô/“¯ˆê‰^”Ô‚ª–¢“o˜^
        if (TrainState == null)
        {
            //1-1.İü‚³‚¹‚é‹O“¹‰ñ˜H‚ÉŠù‚É•Ê‰^“]m‚Ì—ñ”Ô‚ª1‚Â‚Å‚àİü‚µ‚Ä‚¢‚éê‡A‘’…‚Æ‚µ‚Ä“o˜^ˆ—‚µ‚È‚¢B

            //1-2.9999—ñ”Ô‚Ìê‡‚Í—ñÔî•ñ‚ğ“o˜^‚µ‚È‚¢B

            if (clientData.DiaName == "9999")
            {
                // 9999—ñ”Ô‚Í—ñÔî•ñ‚ğ“o˜^‚µ‚È‚¢‚ªAİü‚Í‘‚«‚ŞB     
                await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
                return serverData;
            }
            //1.Š®‘SV‹K“o˜^
            //‘—M‚³‚ê‚½î•ñ‚ÉŠî‚Ã‚¢‚ÄV‹K‚Éî•ñ‚ğ‘‚«‚ŞB

        }
        else
        {
            // “¯ˆê‰^”Ô—ñÔ‚ª“o˜^Ï
            var TrainStateDriverId = TrainState.DriverId;
            // 2.‰^—p’†/•Ê‰^“]m
            if (TrainStateDriverId != null && TrainStateDriverId != clientDriverId)
            {
                // 2.Œğ‘ã‘O‰“š
                // ‘—M‚µ‚Ä‚«‚½ƒNƒ‰ƒCƒAƒ“ƒg‚É‘Î‚µŒğ‘ã‘O‰“š‚ğs‚¢A‘—M‚³‚ê‚½î•ñ‚Íİüî•ñŠÜ‚ß‚Ä‚·‚×‚Ä”jŠü‚·‚éB  
                serverData.IsOnPreviousTrain = true;

                // –hŒì–³ü‚Ìî•ñ‚ÍA‰^—p’†—ñÔ‚Ìİü‹O“¹‰ñ˜H‚ÆƒNƒ‰ƒCƒAƒ“ƒg‚Ìİü‹O“¹‰ñ˜H‚ªŠ®‘Sˆê’v‚µ‚Ä‚¢‚é‚Æ‚«‚Ì‚İ‘—M‚·‚éB
                // ¨Šù‚Éî•ñ‚ª“o˜^‚³‚ê‚Ä‚¢‚é‚½‚ßAã‹L‚Ì‹t‚Ì‚Æ‚«false‚Åã‘‚«‚·‚éB


                return serverData;
            }
            // ‚±‚Ì’n“_‚Åİüî•ñ‚ğ“o˜^‚µ‚Ä‚æ‚¢

            // 3.‰^—pI—¹
            if (TrainStateDriverId == null)
            {
                // 3.î•ñ•ÏX
                // ŒŸõ‚Å”­Œ©‚³‚ê‚½î•ñ‚É‚Â‚¢‚ÄA‘—M‚³‚ê‚½î•ñ‚ÉŠî‚Ã‚¢‚Äî•ñ‚ğ•ÏX‚·‚éB


            }
            // 4.“¯ˆê—ñ”Ô‚ª“o˜^Ï/‰^—p’†/“¯ˆê‰^“]m
            else if (TrainState.TrainNumber == ClientTrainNumber && TrainStateDriverId == clientDriverId)
            {
                // 4.î•ñ•ÏX‚È‚µ
                // —ñÔî•ñ‚É‚Â‚¢‚Ä‚Í•ÏX‚µ‚È‚¢
            }
            else
            {
                // ‚±‚±‚É‚Í—ˆ‚È‚¢
                // ˆÙí‰“š‚È‚Ç‚ğ•Ô‚·‚×‚«
            }
        }

        // İü‹O“¹‰ñ˜H‚ÌXV
        await trackCircuitService.SetTrackCircuitDataList(incrementalTrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(decrementalTrackCircuitDataList);

        // Ô—¼î•ñ‚Ì“o˜^





        return serverData;
    }

    /// <summary>
    /// ‰^”Ô‚ª“¯‚¶‚©‚Ç‚¤‚©‚ğ”»’è‚·‚é
    /// </summary>
    /// <param name="diaName1"></param>
    /// <param name="diaName2"></param>
    /// <returns></returns>
    private bool IsTrainNumberEqual(string diaName1, string diaName2)
    {
        var trainNumber1 = GetTrainNumberFromDiaName(diaName1);
        var trainNumber2 = GetTrainNumberFromDiaName(diaName2);
        return trainNumber1 == trainNumber2;
    }

    /// <summary>
    /// ‰^”Ô‚ğ‹‚ß‚é
    /// </summary>
    /// <param name="diaName"></param>
    /// <returns></returns>
    private int GetTrainNumberFromDiaName(string diaName)
    {
        if (diaName == "9999")
        {
            return 400;
        }
        var isTrain = int.TryParse(Regex.Replace(diaName, @"[^0-9]", ""), out var numBody);  // —ñ”Ô–{‘Ìi”š•”•ªj
        if (isTrain)
        {
            return numBody / 3000 * 100 + numBody % 100;
        }
        // DiaName‚ÌÅŒã‚Ì”š‚ğæ“¾
        return 0;
    }
}
