using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Traincrew_MultiATS_Server.Initialization.CsvLoaders;

/// <summary>
/// 空文字列 → null, それ以外 → そのまま
/// </summary>
public class EmptyStringToNullConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}

/// <summary>
/// 空文字列 → null, 数値文字列 → int?
/// </summary>
public class EmptyStringToNullableIntConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (int.TryParse(text.Trim(), out var value))
            return value;

        return null;
    }
}
