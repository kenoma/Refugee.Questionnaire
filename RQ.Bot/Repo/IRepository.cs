using Microsoft.Extensions.Primitives;
using RQ.DTO;

namespace Bot.Repo;

public interface IRepository
{
    bool TryGetChatData(long chatId, out ChatMetadata chatConfig);
    bool IsKnownToken(string value);
    bool IsKnownTgUser(long userId);
    Questionnaire[] GetAllQuestionaries();
}