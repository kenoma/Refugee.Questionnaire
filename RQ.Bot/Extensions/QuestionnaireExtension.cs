using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using RQ.DTO;

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
                
            };
            
            using var reader = new StreamReader(pathToQuest);
            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<QuestionnaireEntry>()
                .ToArray();

            foreach (var rec in records)
            {
                rec.Text = rec.Text.Trim();
            }

            return new Questionnaire
            {
                Entries = records
            };
        });

        return builder;
    }
}