using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace RQ.Bot.Extensions.CsvUtils;

public class CustomBooleanConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return text?.Equals("1", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}