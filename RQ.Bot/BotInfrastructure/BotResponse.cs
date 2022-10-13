using Newtonsoft.Json;
using RQ.Bot.Domain.Enum;

namespace RQ.Bot.BotInfrastructure;

internal class BotResponse
{
    public BotResponseType E { get; set; } = BotResponseType.None;

    public string P { get; set; }


    public static string Create(BotResponseType entry)
    {
        return JsonConvert.SerializeObject(new BotResponse { E = entry});
    }

    public static string Create(BotResponseType entry, string payload)
    {
        return JsonConvert.SerializeObject(new BotResponse { E = entry, P = payload});
    }
    
    public static string Create(BotResponseType entry, Guid id)
    {
        return JsonConvert.SerializeObject(new BotResponse { E = entry, P = id.ToString("N")});
    }
    
    public static string Create(BotResponseType entry, long userId)
    {
        return JsonConvert.SerializeObject(new BotResponse { E = entry, P = userId.ToString() });
    }
    
    public static string Create(BotResponseType entry, bool switchValue)
    {
        return JsonConvert.SerializeObject(new BotResponse { E = entry, P = switchValue.ToString() });
    }

    public static BotResponse FromString(string callbackQueryData)
    {
        return JsonConvert.DeserializeObject<BotResponse>(callbackQueryData);
    }
}