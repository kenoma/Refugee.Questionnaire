using System.Web;
using Newtonsoft.Json;

namespace RQ.Bot.BotInfrastructure.Entry;

internal class BotResponce
{
    public string E { get; set; } = string.Empty;

    public string P { get; set; }


    public static string Create(string entry)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry});
    }

    public static string Create(string entry, string payload)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry, P = payload});
    }
    
    public static string Create(string entry, Guid id)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry, P = id.ToString("N")});
    }
    
    public static string Create(string entry, long userId)
    {
        return JsonConvert.SerializeObject(new BotResponce { E = entry, P = userId.ToString() });
    }

    public static BotResponce FromString(string callbackQueryData)
    {
        return JsonConvert.DeserializeObject<BotResponce>(callbackQueryData);
    }
}