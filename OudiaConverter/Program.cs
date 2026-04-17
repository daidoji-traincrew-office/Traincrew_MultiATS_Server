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

    [JsonPropertyName("time_range")]
    public string TimeRange { get; set; } = "";

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
    /// oud2ファイルを解析し、ダイヤごとにTTC_Dataを生成
    /// </summary>
    public Dictionary<string, TTC_Data> Convert(string oud2Path)
    {
        var jsonObj = OudiaSecondParser.ParseToJsonCompatibleObject(oud2Path) as Dictionary<string, object>;
        if (jsonObj == null) return new Dictionary<string, TTC_Data>();

        var rosen = GetSingleChild(jsonObj, "Rosen") as Dictionary<string, object>;
        if (rosen == null) return new Dictionary<string, TTC_Data>();

        // 駅リストを取得（順番が重要）
        LoadEkiOrder(rosen);

        // 列車種別を取得
        LoadRessyasyubetsu(rosen);

        // ダイヤを変換
        var result = new Dictionary<string, TTC_Data>();
        var diaList = GetChildList(rosen, "Dia");

        foreach (var diaObj in diaList)
        {
            if (diaObj is not Dictionary<string, object> dia) continue;

            var diaName = GetStringProp(dia, "DiaName");
            if (string.IsNullOrEmpty(diaName)) continue;

            var ttcData = ConvertDia(dia);
            result[diaName] = ttcData;
        }

        return result;
    }

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

        var syubetsuStr = GetStringProp(ressya, "Syubetsu");
        int.TryParse(syubetsuStr, out var syubetsuIndex);
        var trainClass = GetTrainClass(syubetsuIndex);

        var ekiJikokuRaw = GetProp(ressya, "EkiJikoku");
        var ekiJikokuList = ParseEkiJikokuList(ekiJikokuRaw);

        // 駅リストを構築
        var staList = BuildStaList(ekiJikokuList, isKudari);

        if (staList.Count == 0) return null;

        var train = new TTC_Train
        {
            OperationNumber = operationNumber,
            TrainNumber = trainNumber,
            PreviousTrainNumber = "",
            NextTrainNumber = "",
            TrainClass = trainClass,
            OriginStationID = staList.First().StationID,
            OriginStationName = staList.First().StationName,
            DestinationStationID = staList.Last().StationID,
            DestinationStationName = staList.Last().StationName,
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

    private List<TTC_StationData> BuildStaList(List<string> ekiJikokuList, bool isKudari)
    {
        var result = new List<TTC_StationData>();

        // 下りは順方向、上りは逆方向で駅を辿る
        var ekiIndices = Enumerable.Range(0, Math.Min(_ekiOrder.Count, ekiJikokuList.Count));
        if (!isKudari)
        {
            ekiIndices = ekiIndices.Reverse();
        }

        foreach (var i in ekiIndices)
        {
            if (i >= ekiJikokuList.Count) continue;

            var jikokuStr = ekiJikokuList[i];
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
            var outputDir = Path.Combine(outputBaseDir, category);
            Directory.CreateDirectory(outputDir);

            Console.WriteLine();
            Console.WriteLine($"Input:  {oud2Path}");
            Console.WriteLine($"Output: {outputDir}");

            Console.WriteLine("Converting oud2 to TTC_Data...");
            var diaDataMap = converter.Convert(oud2Path);

            Console.WriteLine($"Found {diaDataMap.Count} diagrams:");

            foreach (var (timeRange, ttcData) in diaDataMap)
            {
                ttcData.Version = version;
                ttcData.Name = category;
                ttcData.TimeRange = timeRange;
                var safeName = MakeSafeFileName(timeRange);
                var outputPath = Path.Combine(outputDir, $"{safeName}.json");

                var json = JsonSerializer.Serialize(ttcData, jsonOptions);
                File.WriteAllText(outputPath, json);

                Console.WriteLine($"  - {timeRange}: {ttcData.TrainList.Count} trains → {category}/{safeName}.json");
            }
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
