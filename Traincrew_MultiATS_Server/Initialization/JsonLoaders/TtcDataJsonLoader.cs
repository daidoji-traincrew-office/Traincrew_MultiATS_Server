using System.Text.Json;
using System.Text.Json.Serialization;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.JsonLoaders;

/// <summary>
///     Loader for TTC_Data JSON
/// </summary>
public class TtcDataJsonLoader(ILogger<TtcDataJsonLoader> logger) : BaseJsonLoader<TTC_Data>(logger)
{
    public async Task<TTC_Data?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new TtcStationDataJsonConverter()
            }
        };

        return await LoadJsonAsync(
            CsvFilePaths.TtcDataTrain,
            options,
            cancellationToken);
    }
}

/// <summary>
///     Custom converter for TTC_StationData to handle field name mapping
///     Maps "bansen" JSON property to "stopPosName" field
/// </summary>
internal class TtcStationDataJsonConverter : JsonConverter<TTC_StationData>
{
    public override TTC_StationData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stationData = new TTC_StationData();

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return stationData;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "stationid":
                    stationData.stationID = reader.GetString() ?? "";
                    break;
                case "stationname":
                    stationData.stationName = reader.GetString() ?? "";
                    break;
                case "bansen":
                case "stopposname":
                    stationData.stopPosName = reader.GetString() ?? "";
                    break;
                case "arrivaltime":
                    stationData.arrivalTime = JsonSerializer.Deserialize<TimeOfDay>(ref reader, options);
                    break;
                case "departuretime":
                    stationData.departureTime = JsonSerializer.Deserialize<TimeOfDay>(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Expected EndObject token");
    }

    public override void Write(Utf8JsonWriter writer, TTC_StationData value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialization not required for TTC_StationData");
    }
}
