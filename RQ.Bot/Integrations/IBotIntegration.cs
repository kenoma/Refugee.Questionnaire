using RQ.DTO;

namespace RQ.Bot.Integrations;

public interface IBotIntegration
{
    public Task PushRequestToIntegrationAsync(RefRequest refRequest);
}