using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using RQ.Bot.Domain;
using RQ.Bot.Domain.Enum;
using RQ.Bot.Extensions.CsvUtils;

namespace RQ.Bot.Extensions;

public static class QuestionnaireExtension
{
    public static WebApplicationBuilder UseQuestionnaire(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(_ =>
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
                TrimOptions = TrimOptions.InsideQuotes | TrimOptions.Trim,
                HeaderValidated = null
            };
            
            using var reader = new StreamReader(pathToQuest);
            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<QuestionnaireEntryClassMap>();
            var records = csv.GetRecords<QuestionnaireEntry>();

            var questions = new Questionnaire();

            foreach (var record in records)
            {
                record.Text = record.Text.Trim();
                
                switch (record.AutopassMode)
                {
                    case AutopassMode.None or AutopassMode.Simple: 
                        questions.Entries.Add(record);
                        break;
                    case AutopassMode.Headline:
                        questions.Headliners.Add(record);
                        break;
                    case AutopassMode.Finisher:
                        questions.Finishers.Add(record);
                        break;
                }
            }

            return questions;
        });

        return builder;
    }
}