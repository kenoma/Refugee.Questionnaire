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
                NewLine = Environment.NewLine,
                MissingFieldFound = null,
                IgnoreBlankLines = true,
                BadDataFound = null
            };

            using var reader = new StreamReader(pathToQuest);
            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<QuestionnaireEntry>()
                .ToArray();

            return new Questionnaire
            {
                Entries = records
            };
        });
        
        return builder;
    }
}