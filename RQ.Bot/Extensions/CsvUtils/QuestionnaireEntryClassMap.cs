using CsvHelper.Configuration;
using RQ.DTO;

namespace RQ.Bot.Extensions.CsvUtils;

public sealed class QuestionnaireEntryClassMap : ClassMap<QuestionnaireEntry>
{
    public QuestionnaireEntryClassMap()
    {
        Map(p => p.Category);
        Map(p => p.Group).TypeConverter<CustomIntegerConverter>();
        Map(p => p.Text);
        Map(p => p.DuplicateCheck).TypeConverter<CustomByteConverter>();
        Map(p => p.ValidationRegex);
        Map(p => p.IsAutoPass).TypeConverter<CustomBooleanConverter>();
        Map(p => p.IsGroupSwitch).TypeConverter<CustomByteConverter>();
    }
}