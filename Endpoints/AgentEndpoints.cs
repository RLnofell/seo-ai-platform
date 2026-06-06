using AI_SEO_Ssas_Platform.Services;

namespace AI_SEO_Ssas_Platform.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => "AI SEO Agent API is running with SignalR support!");

        endpoints.MapPost("/api/agent/run", async (RunRequest request, IAgentOrchestrator orchestrator) =>
        {
            var result = await orchestrator.RunAgentAsync(request.Input, request.ConnectionId, request.Language);
            return Results.Ok(result);
        });

        return endpoints;
    }
}

public record RunRequest(string Input, string ConnectionId, string Language = "vi");
