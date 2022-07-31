using CsvHelper.Configuration;
using RQ.DTO;

namespace RQ.Bot.Extensions.CsvUtils;

public sealed class QuestionnaireEntryClassMap : ClassMap<QuestionnaireEntry>
{
    public QuestionnaireEntryClassMap()
    {
        Map(p => p.Category);
        Map(p => p.Group);
        Map(p => p.Text);
        Map(p => p.DuplicateCheck);
        Map(p => p.ValidationRegex);
        Map(p => p.IsAutoPass).TypeConverter<CustomBooleanConverter>();
        Map(p => p.IsGroupSwitch);
    }
}