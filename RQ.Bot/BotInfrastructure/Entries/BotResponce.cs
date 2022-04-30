using Newtonsoft.Json;

namespace RQ.Bot.BotInfrastructure.Entry;

internal class BotResponce
{
    public string Entry { get; set; } = string.Empty;

    public long Id { get; set; }

    public string Payload { get; set; }


    public static string Create(string entry)
    {
        return JsonConvert.SerializeObject(new BotResponce { Entry = entry});
    }

    public static string Create(string entry, int id, string payload)
    {
        return JsonConvert.SerializeObject(new BotResponce { Entry = entry, Id = id, Payload = payload });
    }

    public static string Create(string entry, long id, string payload)
    {
        return JsonConvert.SerializeObject(new BotResponce { Entry = entry, Id = id, Payload = payload });
    }

    public static BotResponce FromString(string callbackQueryData)
    {
        return JsonConvert.DeserializeObject<BotResponce>(callbackQueryData);
    }
}