using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace RQ.Bot.Extensions.CsvUtils;

public class CustomIntegerConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return int.TryParse(text, out var result) ? result : 0;
    }
}

public class CustomByteConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return byte.TryParse(text, out var result) ? result : (byte)0;
    }
}

public class CustomEnumConverter<T> : DefaultTypeConverter where T : struct, Enum
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return Enum.TryParse<T>(text, out var parsedEnum) ? parsedEnum : default(T);
    }
}