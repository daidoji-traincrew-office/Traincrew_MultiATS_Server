using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ConsoleApp1;

/// <summary>
/// OuDiaSecond(.oud2)形式のテキストをJSON互換構造にパースする
/// </summary>
public static class OudiaSecondParser
{
    /// <summary>
    /// ノード（階層）を表す
    /// </summary>
    private sealed class Node
    {
        public string Name { get; }

        // 値は string か List<string> を入れる（JSON互換）
        public Dictionary<string, List<object>> Props { get; } = new();

        public Dictionary<string, List<Node>> Children { get; } = new();

        public Node(string name) => Name = name;

        public void AddProp(string key, object value)
        {
            if (!Props.TryGetValue(key, out var list))
            {
                list = [];
                Props[key] = list;
            }
            list.Add(value);
        }

        public void AddChild(Node child)
        {
            if (!Children.TryGetValue(child.Name, out var list))
            {
                list = [];
                Children[child.Name] = list;
            }
            list.Add(child);
        }
    }

    /// <summary>
    /// ファイルを読み込んで、階層構造をパースする
    /// </summary>
    public static object ParseToJsonCompatibleObject(string filePath)
    {
        using var sr = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);

        var root = new Node("Root");
        var stack = new Stack<Node>();
        stack.Push(root);

        while (!sr.EndOfStream)
        {
            var raw = sr.ReadLine();
            if (raw == null) continue;

            var line = raw.Trim();
            if (line.Length == 0) continue;

            // 階層終了
            if (line == ".")
            {
                if (stack.Count > 1) stack.Pop();
                continue;
            }

            // 階層開始（末尾がドット）
            if (line.EndsWith(".", StringComparison.Ordinal))
            {
                var name = line.Substring(0, line.Length - 1).Trim();
                if (name.Length == 0) continue;

                var child = new Node(name);
                stack.Peek().AddChild(child);
                stack.Push(child);
                continue;
            }

            // key=value
            var eqIndex = line.IndexOf('=');
            if (eqIndex >= 1)
            {
                var key = line.Substring(0, eqIndex).Trim();
                var valueRaw = line.Substring(eqIndex + 1); // value側は空白も含める

                var parsedValue = ParseValueCommaArray(valueRaw);
                stack.Peek().AddProp(key, parsedValue);
            }
            // それ以外の行は無視（必要ならログ出し可）
        }

        return ConvertNodeToJsonCompatible(root);
    }

    /// <summary>
    /// valueを "," を区切りとして配列化して返す（なければ文字列）
    /// </summary>
    private static object ParseValueCommaArray(string valueRaw)
    {
        // そのままが良いケース（空文字など）
        if (valueRaw == null) return "";

        // カンマがなければ文字列で返す
        if (!valueRaw.Contains(',')) return valueRaw;

        // カンマがあれば配列として返す（空要素も保持）
        var parts = valueRaw.Split(',')
            .Select(x => x.Trim())
            .ToList();

        return parts;
    }

    /// <summary>
    /// JSON互換になる形（Dictionary/List/string）に変換する
    /// </summary>
    private static object ConvertNodeToJsonCompatible(Node node)
    {
        var dict = new Dictionary<string, object>();

        // プロパティ
        foreach (var kv in node.Props)
        {
            // keyが1回だけならその値（string or List<string>）を直置き
            // 複数回なら配列にする（要素が配列になることもある）
            dict[kv.Key] = kv.Value.Count == 1 ? kv.Value[0] : kv.Value.ToList();
        }

        // 子要素
        foreach (var ck in node.Children)
        {
            var children = ck.Value.Select(ConvertNodeToJsonCompatible).ToList();
            dict[ck.Key] = children.Count == 1 ? children[0] : children;
        }

        return dict;
    }

    /// <summary>
    /// パースして、整形済みJSON文字列にする
    /// </summary>
    public static string ParseToJsonString(string filePath)
    {
        var obj = ParseToJsonCompatibleObject(filePath);

        var opt = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 日本語を \u にしない
        };

        return JsonSerializer.Serialize(obj, opt);
    }
}

#region TTC_Data Model Classes

/// <summary>
/// TTC_Data: 列車ダイヤ全体のデータ
/// </summary>
public class TTC_Data
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("TrainList")]
    public List<TTC_Train> TrainList { get; set; } = [];
}

/// <summary>
/// TTC_Train: 列車ごとの情報
/// </summary>
public class TTC_Train
{
    [JsonPropertyName("operationNumber")]
    public int OperationNumber { get; set; }

    [JsonPropertyName("trainNumber")]
    public string TrainNumber { get; set; } = "";

    [JsonPropertyName("previousTrainNumber")]
    public string PreviousTrainNumber { get; set; } = "";

    [JsonPropertyName("nextTrainNumber")]
    public string NextTrainNumber { get; set; } = "";

    [JsonPropertyName("trainClass")]
    public string TrainClass { get; set; } = "";

    [JsonPropertyName("originStationID")]
    public string OriginStationID { get; set; } = "";

    [JsonPropertyName("originStationName")]
    public string OriginStationName { get; set; } = "";

    [JsonPropertyName("destinationStationID")]
    public string DestinationStationID { get; set; } = "";

    [JsonPropertyName("destinationStationName")]
    public string DestinationStationName { get; set; } = "";

    [JsonPropertyName("staList")]
    public List<TTC_StationData> StaList { get; set; } = [];

    [JsonPropertyName("temporaryStopStations")]
    public string[] TemporaryStopStations { get; set; } = [];

    [JsonPropertyName("isRegularService")]
    public bool IsRegularService { get; set; } = true;

    [JsonPropertyName("trainName")]
    public string? TrainName { get; set; }

    [JsonPropertyName("carCount")]
    public int CarCount { get; set; } = 4;
}

/// <summary>
/// TTC_StationData: 停車駅の情報
/// </summary>
public class TTC_StationData
{
    [JsonPropertyName("stationID")]
    public string StationID { get; set; } = "";

    [JsonPropertyName("stationName")]
    public string StationName { get; set; } = "";

    [JsonPropertyName("stopPosName")]
    public string StopPosName { get; set; } = "";

    [JsonPropertyName("arrivalTime")]
    public TimeOfDay? ArrivalTime { get; set; }

    [JsonPropertyName("departureTime")]
    public TimeOfDay? DepartureTime { get; set; }
}

/// <summary>
/// TimeOfDay: 時刻の情報
/// </summary>
public class TimeOfDay
{
    [JsonPropertyName("h")]
    public int H { get; set; }

    [JsonPropertyName("m")]
    public int M { get; set; }

    [JsonPropertyName("s")]
    public int S { get; set; }
}

#endregion

/// <summary>
/// OuDiaSecond (.oud2) → TTC_Data 変換器
/// </summary>
public class Oud2ToTtcConverter
{
    private readonly Dictionary<string, string> _stationNameToId = new();
    private readonly List<string> _ekiOrder = [];
    private readonly Dictionary<int, string> _syubetsuIndexToName = new();

    /// <summary>
    /// 駅・停車場CSVを読み込み、駅名→駅IDマッピングを構築
    /// </summary>
    public void LoadStationMapping(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        // ヘッダースキップ
        reader.ReadLine();

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length >= 2)
            {
                var stationId = parts[0].Trim();
                var stationName = parts[1].Trim();
                _stationNameToId[stationName] = stationId;

                // 表記揺れ対応: ケ/ヶ, ガ/が 等を両方登録
                var normalized = NormalizeStationName(stationName);
                if (normalized != stationName)
                {
                    _stationNameToId[normalized] = stationId;
                }
            }
        }
    }

    /// <summary>
    /// 駅名を正規化（表記揺れ対応）
    /// </summary>
    private static string NormalizeStationName(string name)
    {
        // ケ/ヶ の相互変換
        return name
            .Replace('ケ', 'ヶ')
            .Replace('ガ', 'が')
            .Replace('ヅ', 'づ');
    }

    /// <summary>
    /// 駅名から駅IDを取得（正規化による検索も試みる）
    /// </summary>
    private string GetStationId(string stationName)
    {
        if (_stationNameToId.TryGetValue(stationName, out var id))
        {
            return id;
        }

        // 正規化して再検索
        var normalized = NormalizeStationName(stationName);
        if (_stationNameToId.TryGetValue(normalized, out id))
        {
            return id;
        }

        return "";
    }

    /// <summary>
    /// oud2ファイルを解析し、全ダイヤをマージした単一の TTC_Data を生成
    /// </summary>
    public TTC_Data Convert(string oud2Path)
    {
        var jsonObj = OudiaSecondParser.ParseToJsonCompatibleObject(oud2Path) as Dictionary<string, object>;
        if (jsonObj == null) return new TTC_Data();

        var rosen = GetSingleChild(jsonObj, "Rosen") as Dictionary<string, object>;
        if (rosen == null) return new TTC_Data();

        LoadEkiOrder(rosen);
        LoadRessyasyubetsu(rosen);

        var allTrains = new List<TTC_Train>();
        var diaList = GetChildList(rosen, "Dia");

        foreach (var diaObj in diaList)
        {
            if (diaObj is not Dictionary<string, object> dia) continue;
            var ttcData = ConvertDia(dia);
            allTrains.AddRange(ttcData.TrainList);
        }

        return MergeAllTrains(allTrains);
    }

    /// <summary>
    /// 全列車を列番でグループ化してマージし、単一の TTC_Data にまとめる
    /// </summary>
    private TTC_Data MergeAllTrains(List<TTC_Train> allTrains)
    {
        var result = new TTC_Data();
        int operationNumber = 1;

        var groups = allTrains
            .Where(t => !string.IsNullOrWhiteSpace(t.TrainNumber))
            .GroupBy(t => t.TrainNumber);

        foreach (var group in groups)
        {
            var merged = MergeTrains(group.Key, group.ToList());
            merged.OperationNumber = operationNumber++;
            result.TrainList.Add(merged);
        }

        return result;
    }

    /// <summary>
    /// 同一列番の列車リストを staList の union でマージする
    /// </summary>
    private TTC_Train MergeTrains(string trainNumber, List<TTC_Train> trains)
    {
        if (trains.Count == 1) return trains[0];

        // 最初の有効な列車から進行方向を判定
        var refTrain = trains.First(t => t.StaList.Count > 0);
        var firstIdx = _ekiOrder.IndexOf(refTrain.StaList.First().StationName);
        var lastIdx = _ekiOrder.IndexOf(refTrain.StaList.Last().StationName);
        bool isKudari = firstIdx <= lastIdx;

        // 駅ごとの情報をマージ（stationID or stationName をキーに union）
        var stationMap = new Dictionary<string, TTC_StationData>(StringComparer.Ordinal);

        foreach (var train in trains)
        {
            foreach (var sta in train.StaList)
            {
                var key = !string.IsNullOrEmpty(sta.StationID) ? sta.StationID : sta.StationName;
                if (!stationMap.TryGetValue(key, out var existing))
                {
                    stationMap[key] = new TTC_StationData
                    {
                        StationID = sta.StationID,
                        StationName = sta.StationName,
                        StopPosName = sta.StopPosName,
                        ArrivalTime = sta.ArrivalTime,
                        DepartureTime = sta.DepartureTime
                    };
                }
                else
                {
                    // 情報補完: null の場合は新しい値を採用、競合は先勝ちで警告
                    if (existing.ArrivalTime == null && sta.ArrivalTime != null)
                        existing.ArrivalTime = sta.ArrivalTime;
                    else if (existing.ArrivalTime != null && sta.ArrivalTime != null
                             && !TimesEqual(existing.ArrivalTime, sta.ArrivalTime))
                        Console.Error.WriteLine(
                            $"[WARN] {trainNumber}: {sta.StationName} 到着時刻競合 "
                            + $"{FormatTime(existing.ArrivalTime)} vs {FormatTime(sta.ArrivalTime)} → 先勝ち");

                    if (existing.DepartureTime == null && sta.DepartureTime != null)
                        existing.DepartureTime = sta.DepartureTime;
                    else if (existing.DepartureTime != null && sta.DepartureTime != null
                             && !TimesEqual(existing.DepartureTime, sta.DepartureTime))
                        Console.Error.WriteLine(
                            $"[WARN] {trainNumber}: {sta.StationName} 出発時刻競合 "
                            + $"{FormatTime(existing.DepartureTime)} vs {FormatTime(sta.DepartureTime)} → 先勝ち");

                    if (string.IsNullOrEmpty(existing.StopPosName) && !string.IsNullOrEmpty(sta.StopPosName))
                        existing.StopPosName = sta.StopPosName;
                }
            }
        }

        // 駅を進行方向順にソート
        var sortedStaList = stationMap.Values
            .OrderBy(s =>
            {
                var idx = _ekiOrder.IndexOf(s.StationName);
                return isKudari ? idx : -idx;
            })
            .ToList();

        var origin = sortedStaList.First();
        var dest = sortedStaList.Last();

        return new TTC_Train
        {
            OperationNumber = 0,
            TrainNumber = trainNumber,
            PreviousTrainNumber = "",
            NextTrainNumber = "",
            TrainName = trains.Select(t => t.TrainName).FirstOrDefault(n => !string.IsNullOrEmpty(n)),
            TrainClass = trains.Select(t => t.TrainClass).FirstOrDefault(tc => !string.IsNullOrEmpty(tc)) ?? "",
            OriginStationID = origin.StationID,
            OriginStationName = origin.StationName,
            DestinationStationID = dest.StationID,
            DestinationStationName = dest.StationName,
            StaList = sortedStaList,
            TemporaryStopStations = [],
            IsRegularService = trains.All(t => t.IsRegularService),
            CarCount = trains.First().CarCount
        };
    }

    private static bool TimesEqual(TimeOfDay a, TimeOfDay b)
        => a.H == b.H && a.M == b.M && a.S == b.S;

    private static string FormatTime(TimeOfDay t)
        => $"{t.H:00}:{t.M:00}:{t.S:00}";

    private void LoadEkiOrder(Dictionary<string, object> rosen)
    {
        _ekiOrder.Clear();
        var ekiList = GetChildList(rosen, "Eki");

        foreach (var ekiObj in ekiList)
        {
            if (ekiObj is not Dictionary<string, object> eki) continue;
            var ekimei = GetStringProp(eki, "Ekimei");
            if (!string.IsNullOrEmpty(ekimei))
            {
                _ekiOrder.Add(ekimei);
            }
        }
    }

    private void LoadRessyasyubetsu(Dictionary<string, object> rosen)
    {
        _syubetsuIndexToName.Clear();
        var syubetsuList = GetChildList(rosen, "Ressyasyubetsu");

        for (int i = 0; i < syubetsuList.Count; i++)
        {
            if (syubetsuList[i] is not Dictionary<string, object> syubetsu) continue;
            var name = GetStringProp(syubetsu, "Syubetsumei");
            if (!string.IsNullOrEmpty(name))
            {
                _syubetsuIndexToName[i] = name;
            }
        }
    }

    private TTC_Data ConvertDia(Dictionary<string, object> dia)
    {
        var ttcData = new TTC_Data();
        int operationNumber = 1;

        // 下り列車
        var kudari = GetSingleChild(dia, "Kudari") as Dictionary<string, object>;
        if (kudari != null)
        {
            var ressyaList = GetChildList(kudari, "Ressya");
            foreach (var ressyaObj in ressyaList)
            {
                if (ressyaObj is not Dictionary<string, object> ressya) continue;
                var train = ConvertRessya(ressya, isKudari: true, operationNumber++);
                if (train != null)
                {
                    ttcData.TrainList.Add(train);
                }
            }
        }

        // 上り列車
        var nobori = GetSingleChild(dia, "Nobori") as Dictionary<string, object>;
        if (nobori != null)
        {
            var ressyaList = GetChildList(nobori, "Ressya");
            foreach (var ressyaObj in ressyaList)
            {
                if (ressyaObj is not Dictionary<string, object> ressya) continue;
                var train = ConvertRessya(ressya, isKudari: false, operationNumber++);
                if (train != null)
                {
                    ttcData.TrainList.Add(train);
                }
            }
        }

        return ttcData;
    }

    private TTC_Train? ConvertRessya(Dictionary<string, object> ressya, bool isKudari, int operationNumber)
    {
        var trainNumber = GetStringProp(ressya, "Ressyabangou");
        if (string.IsNullOrEmpty(trainNumber)) return null;

        var ressyamei = GetStringProp(ressya, "Ressyamei");

        var syubetsuStr = GetStringProp(ressya, "Syubetsu");
        int.TryParse(syubetsuStr, out var syubetsuIndex);
        var trainClass = GetTrainClass(syubetsuIndex);

        // B=始発・A=終着（上下共通）。上りは rawIdx を逆変換して _ekiOrder インデックスに変換
        var (originName, originId, originEkiIdx, _) = GetOperationStation(ressya, 'B', isKudari, trainNumber);
        var (destName, destId, destEkiIdx, _) = GetOperationStation(ressya, 'A', isKudari, trainNumber);

        var startEkiIdx = Math.Min(originEkiIdx, destEkiIdx);
        var endEkiIdx = Math.Max(originEkiIdx, destEkiIdx);

        var ekiJikokuRaw = GetProp(ressya, "EkiJikoku");
        var ekiJikokuList = ParseEkiJikokuList(ekiJikokuRaw);

        // 始発/終着の範囲内の駅リストを構築
        var staList = BuildStaList(ekiJikokuList, isKudari, startEkiIdx, endEkiIdx);

        if (staList.Count == 0) return null;

        var train = new TTC_Train
        {
            OperationNumber = operationNumber,
            TrainNumber = trainNumber,
            PreviousTrainNumber = "",
            NextTrainNumber = "",
            TrainName = string.IsNullOrEmpty(ressyamei) ? null : ressyamei,
            TrainClass = trainClass,
            OriginStationID = originId,
            OriginStationName = originName,
            DestinationStationID = destId,
            DestinationStationName = destName,
            StaList = staList,
            TemporaryStopStations = [],
            IsRegularService = true,
            CarCount = 4
        };

        return train;
    }

    private string GetTrainClass(int syubetsuIndex)
    {
        if (_syubetsuIndexToName.TryGetValue(syubetsuIndex, out var name))
        {
            // "(時刻変更)" を除去
            if (name.Contains("(時刻変更)"))
            {
                name = name.Replace("(時刻変更)", "");
            }
            return name;
        }
        return "普通";
    }

    private List<string> ParseEkiJikokuList(object? raw)
    {
        if (raw is List<object> list)
        {
            return list.Select(o => o?.ToString() ?? "").ToList();
        }
        if (raw is List<string> strList)
        {
            return strList;
        }
        if (raw is string s)
        {
            return s.Split(',').ToList();
        }
        return [];
    }

    private List<TTC_StationData> BuildStaList(List<string> ekiJikokuList, bool isKudari, int startEkiIdx, int endEkiIdx)
    {
        var result = new List<TTC_StationData>();

        // 下りは順方向、上りは逆方向で _ekiOrder を辿る
        var ekiIndices = Enumerable.Range(0, _ekiOrder.Count);
        if (!isKudari)
        {
            ekiIndices = ekiIndices.Reverse();
        }

        foreach (var i in ekiIndices)
        {
            if (i < startEkiIdx || i > endEkiIdx) continue;

            // 上り列車の EkiJikoku は「上り方向のインデックス(逆順)」で格納されている
            var jikokuIdx = isKudari ? i : _ekiOrder.Count - 1 - i;
            if (jikokuIdx >= ekiJikokuList.Count) continue;

            var jikokuStr = ekiJikokuList[jikokuIdx];
            if (string.IsNullOrEmpty(jikokuStr)) continue;

            var ekimei = _ekiOrder[i];
            var stationId = GetStationId(ekimei);

            var stationData = ParseEkiJikoku(jikokuStr, ekimei, stationId);
            if (stationData != null)
            {
                result.Add(stationData);
            }
        }

        return result;
    }

    /// <summary>
    /// EkiJikoku文字列を解析して TTC_StationData を返す
    /// 形式: 駅扱い;着時刻/発時刻$番線 または 駅扱い$番線（時刻なし）
    /// </summary>
    private TTC_StationData? ParseEkiJikoku(string jikokuStr, string stationName, string stationId)
    {
        // 空文字なら該当なし
        if (string.IsNullOrWhiteSpace(jikokuStr)) return null;

        // 駅扱い、時刻部、番線に分解
        // 例: "1;52130$1" → 扱い=1, 時刻="52130", 番線="1"
        // 例: "1;52305/52335$2" → 扱い=1, 着="52305", 発="52335", 番線="2"
        // 例: "2$1" → 扱い=2, 時刻=null, 番線="1"

        var ekiAtsukai = "";
        var timesPart = "";
        var trackPart = "";

        // まず $ で番線を分離
        var dollarIdx = jikokuStr.IndexOf('$');
        string mainPart;
        if (dollarIdx >= 0)
        {
            mainPart = jikokuStr.Substring(0, dollarIdx);
            trackPart = jikokuStr.Substring(dollarIdx + 1);
        }
        else
        {
            mainPart = jikokuStr;
        }

        // ; で駅扱いと時刻を分離
        var semicolonIdx = mainPart.IndexOf(';');
        if (semicolonIdx >= 0)
        {
            ekiAtsukai = mainPart.Substring(0, semicolonIdx);
            timesPart = mainPart.Substring(semicolonIdx + 1);
        }
        else
        {
            ekiAtsukai = mainPart;
            timesPart = "";
        }

        // 駅扱いが空でもOK（パターンによっては）
        // 着時刻・発時刻を解析
        TimeOfDay? arrivalTime = null;
        TimeOfDay? departureTime = null;

        if (!string.IsNullOrEmpty(timesPart))
        {
            var slashIdx = timesPart.IndexOf('/');
            if (slashIdx >= 0)
            {
                // 着/発
                var arrStr = timesPart.Substring(0, slashIdx);
                var depStr = timesPart.Substring(slashIdx + 1);
                arrivalTime = ParseTime(arrStr);
                departureTime = ParseTime(depStr);
            }
            else
            {
                // 発時刻のみ（または通過時刻）
                departureTime = ParseTime(timesPart);
            }
        }

        // 番線からstopPosNameを構築
        var stopPosName = !string.IsNullOrEmpty(trackPart) ? $"{trackPart}番線" : "";

        return new TTC_StationData
        {
            StationID = stationId,
            StationName = stationName,
            StopPosName = stopPosName,
            ArrivalTime = arrivalTime,
            DepartureTime = departureTime
        };
    }

    /// <summary>
    /// oud2形式の時刻文字列を TimeOfDay に変換
    /// 形式: HMMSS または HHMM 形式
    /// 例: "52130" = 5:21:30, "525" = 5:25:00, "101530" = 10:15:30
    /// </summary>
    private TimeOfDay? ParseTime(string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr)) return null;

        // 数字のみ抽出
        var digits = new string(timeStr.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return null;

        int h, m, s = 0;

        // 桁数に応じて解析
        // 1-2桁: 時のみ (例: "5" → 5:00:00)
        // 3桁: H:MM (例: "525" → 5:25:00)
        // 4桁: HH:MM or H:MM:SS の判断が難しい → HH:MM と解釈
        // 5桁: H:MM:SS (例: "52130" → 5:21:30)
        // 6桁: HH:MM:SS (例: "101530" → 10:15:30)
        switch (digits.Length)
        {
            case 1:
            case 2:
                h = int.Parse(digits);
                m = 0;
                break;
            case 3:
                h = int.Parse(digits.Substring(0, 1));
                m = int.Parse(digits.Substring(1, 2));
                break;
            case 4:
                h = int.Parse(digits.Substring(0, 2));
                m = int.Parse(digits.Substring(2, 2));
                break;
            case 5:
                h = int.Parse(digits.Substring(0, 1));
                m = int.Parse(digits.Substring(1, 2));
                s = int.Parse(digits.Substring(3, 2));
                break;
            case 6:
                h = int.Parse(digits.Substring(0, 2));
                m = int.Parse(digits.Substring(2, 2));
                s = int.Parse(digits.Substring(4, 2));
                break;
            default:
                return null;
        }

        return new TimeOfDay { H = h, M = m, S = s };
    }

    private static readonly Regex OperationKeyRegex = new(@"^Operation(\d+)([AB])$", RegexOptions.Compiled);

    /// <summary>
    /// Operation(\d+)A/B キーから始発/終着駅の名前とIDを取得する
    /// </summary>
    private (string stationName, string stationId, int ekiIdx, int rawIdx) GetOperationStation(
        Dictionary<string, object> ressya, char suffix, bool isKudari, string trainNumber)
    {
        foreach (var key in ressya.Keys)
        {
            var m = OperationKeyRegex.Match(key);
            if (!m.Success || m.Groups[2].Value[0] != suffix) continue;

            var rawIdx = int.Parse(m.Groups[1].Value);
            var ekiIdx = isKudari ? rawIdx : _ekiOrder.Count - 1 - rawIdx;

            if (ekiIdx < 0 || ekiIdx >= _ekiOrder.Count)
                throw new InvalidOperationException(
                    $"列車 {trainNumber}: キー {key} の駅インデックス {rawIdx} が範囲外です (駅数={_ekiOrder.Count})");

            var stationName = _ekiOrder[ekiIdx];
            return (stationName, GetStationId(stationName), ekiIdx, rawIdx);
        }

        throw new InvalidOperationException(
            $"列車 {trainNumber}: Operation*{suffix} キーが見つかりません");
    }

    #region Helper methods

    private static object? GetSingleChild(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return null;

        if (value is List<object> list && list.Count > 0)
        {
            return list[0];
        }
        return value;
    }

    private static List<object> GetChildList(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return [];

        if (value is List<object> list)
        {
            return list;
        }
        return [value];
    }

    private static string GetStringProp(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return "";
        return value?.ToString() ?? "";
    }

    private static object? GetProp(Dictionary<string, object> dict, string key)
    {
        dict.TryGetValue(key, out var value);
        return value;
    }

    #endregion
}

/// <summary>
/// メインエントリーポイント
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var baseDir = AppContext.BaseDirectory;

        // プロジェクトルートを探す（binフォルダから遡る）
        var projectDir = FindProjectRoot(baseDir);
        if (projectDir == null)
        {
            Console.WriteLine("Error: Could not find project root.");
            return;
        }

        var inputDir = Path.Combine(projectDir, "OudiaConverter", "input");
        var crewDataDir = Path.Combine(projectDir, "Traincrew_MultiATS_Server.Crew", "Data");
        var stationCsvPath = Path.Combine(crewDataDir, "駅・停車場.csv");
        var outputBaseDir = Path.Combine(crewDataDir, "Diagram");

        if (!File.Exists(stationCsvPath))
        {
            Console.WriteLine($"Error: Station CSV not found: {stationCsvPath}");
            return;
        }

        // 正規表現で対象ファイルを列挙
        var filePattern = new Regex(@"^館浜(.+)信号v(\d{4})\.oud2$");
        var oud2Files = Directory.GetFiles(inputDir, "*.oud2")
            .Select(f => (Path: f, Match: filePattern.Match(Path.GetFileName(f))))
            .Where(x => x.Match.Success)
            .ToList();

        if (oud2Files.Count == 0)
        {
            Console.WriteLine($"Error: No matching oud2 files found in: {inputDir}");
            return;
        }

        Console.WriteLine("=== OuDiaSecond (.oud2) → TTC_Data Converter ===");

        // コンバーター初期化
        var converter = new Oud2ToTtcConverter();

        Console.WriteLine("Loading station mapping...");
        converter.LoadStationMapping(stationCsvPath);

        // JSON出力オプション
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        foreach (var (oud2Path, match) in oud2Files)
        {
            var category = match.Groups[1].Value;   // $1 e.g. "運転会"
            var version = match.Groups[2].Value;    // $2 e.g. "0414"
            var legacyDir = Path.Combine(outputBaseDir, category);

            Console.WriteLine();
            Console.WriteLine($"Input:  {oud2Path}");

            // 旧形式のサブディレクトリ（時間帯別JSONファイル群）を削除
            if (Directory.Exists(legacyDir))
            {
                Directory.Delete(legacyDir, recursive: true);
                Console.WriteLine($"Removed legacy directory: {legacyDir}");
            }

            Console.WriteLine("Converting oud2 to TTC_Data...");
            var ttcData = converter.Convert(oud2Path);
            ttcData.Version = version;
            ttcData.Name = category;

            var outputPath = Path.Combine(outputBaseDir, $"{category}.json");
            var json = JsonSerializer.Serialize(ttcData, jsonOptions);
            File.WriteAllText(outputPath, json);

            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine($"  {ttcData.TrainList.Count} trains merged → {category}.json");
        }

        Console.WriteLine();
        Console.WriteLine("Conversion complete!");
    }

    private static string? FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            // .slnファイルまたはConsoleApp1フォルダがあればプロジェクトルート
            if (File.Exists(Path.Combine(dir.FullName, "Traincrew_MultiATS_Server.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static string MakeSafeFileName(string name)
    {
        // ファイル名に使えない文字を置換
        var invalid = Path.GetInvalidFileNameChars();
        var result = name;
        foreach (var c in invalid)
        {
            result = result.Replace(c, '_');
        }
        return result;
    }
}
