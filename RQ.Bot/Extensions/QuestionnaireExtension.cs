using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using RQ.Bot.Extensions.CsvUtils;
using RQ.DTO;
using RQ.DTO.Enum;

namespace RQ.Bot.Extensions;

public static class QuestionnaireExtension
{
    public static WebApplicationBuilder UseQuestionnaire(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(z =>
        {
            var pathToQuest = builder.Configuration["pathToQuest"];

            if (string.IsNullOrWhiteSpace(pathToQuest))
                throw new InvalidProgramException("Specify --pathToQuest argument");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                MissingFieldFound = null,
                IgnoreBlankLines = true,
                BadDataFound = null,
                DetectDelimiter = true,
                DetectDelimiterValues = new[] { ",", ";", "\t" },
                TrimOptions = TrimOptions.InsideQuotes| TrimOptions.Trim,
                HeaderValidated = null
            };
            
            using var reader = new StreamReader(pathToQuest);
            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<QuestionnaireEntryClassMap>();
            var records = csv.GetRecords<QuestionnaireEntry>()
                .ToArray();

            foreach (var rec in records)
            {
                rec.Text = rec.Text.Trim();
            }

            var questionnaire = new Questionnaire
            {
                Entries = records
                    .Where(z => z.AutopassMode is AutopassMode.None or AutopassMode.Simple).ToArray(),
                Headliners = records.Where(z => z.AutopassMode == AutopassMode.Headline).ToArray(),
                Finishers = records.Where(z => z.AutopassMode == AutopassMode.Finisher).ToArray(),
            };
            return questionnaire;
        });

        return builder;
    }
}