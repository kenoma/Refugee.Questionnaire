using System.Web;
using Newtonsoft.Json;

namespace RQ.Bot.BotInfrastructure.Entry;

internal class BotResponce
{
    public string Entry { get; set; } = string.Empty;

    public string Id { get; set; }


    public static string Create(string entry)
    {
        return JsonConvert.SerializeObject(new BotResponce { Entry = entry});
    }

    public static string Create(string entry, Guid id)
    {
        return JsonConvert.SerializeObject(new BotResponce { Entry = entry, Id = id.ToString("N")});
    }

    public static BotResponce FromString(string callbackQueryData)
    {
        return JsonConvert.DeserializeObject<BotResponce>(callbackQueryData);
    }
}