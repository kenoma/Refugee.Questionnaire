using Newtonsoft.Json;
using RQ.DTO.Enum;

namespace RQ.Bot.BotInfrastructure.Entry;

internal class BotResponce
{
    public BotResponceType E { get; set; } = BotResponceType.None;

    public string P { get; set; }


    public static string Create(BotResponceType entry)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry});
    }

    public static string Create(BotResponceType entry, string payload)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry, P = payload});
    }
    
    public static string Create(BotResponceType entry, Guid id)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry, P = id.ToString("N")});
    }
    
    public static string Create(BotResponceType entry, long userId)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry, P = userId.ToString() });
    }
    
    public static string Create(BotResponceType entry, bool switchValue)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry, P = switchValue.ToString() });
    }

    public static BotResponce FromString(string callbackQueryData)
    {
        return JsonConvert.DeserializeObject<BotResponce>(callbackQueryData);
    }
}