using Newtonsoft.Json;
using RQ.Bot.Domain;
using RQ.Bot.Domain.Enum;
using RQ.Bot.Extensions.Config;

namespace RQ.Bot.Extensions;

public static class QuestionnaireExtension
{
    public static WebApplicationBuilder UseQuestionnaire(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(_ =>
        {
            var rawQuestData = builder.Configuration["questionnaireRaw"];

            if (string.IsNullOrWhiteSpace(rawQuestData))
                throw new InvalidProgramException("Specify --questionnaireRaw argument");

            var rawQuestItems = JsonConvert.DeserializeObject<Question[]>(rawQuestData);
            
            var questions = new Questionnaire();

            foreach (var rawRecord in rawQuestItems.OrderBy(z=>z.OrderPosition))
            {
                var record = rawRecord.ToQuestionnaireEntry();
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