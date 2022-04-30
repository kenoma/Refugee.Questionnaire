using RQ.DTO;

namespace Bot.Repo;

public interface IRepository
{
    bool TryGetChatData(long chatId, out ChatConfiguration chatConfig);
}