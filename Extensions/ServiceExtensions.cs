using AI_SEO_Ssas_Platform.Services;
using AI_SEO_Ssas_Platform.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Sqlite;

namespace AI_SEO_Ssas_Platform.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ILogCollector, LogCollector>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddScoped<RagPlugin>();
        services.AddScoped<SeoAutomationPlugin>();

        services.AddSingleton<ISemanticTextMemory>(sp => 
        {
#pragma warning disable SKEXP0001
#pragma warning disable CS0618
            string ollamaUrl = config["AI:Endpoint"]?.Replace("/v1", "") ?? "http://localhost:11434";
            string embedModel = config["AI:EmbeddingModelId"] ?? "nomic-embed-text";
            string dbConnection = config["Database:VectorDbConnectionString"] ?? "vector_database.db";
            
            var customOllamaEmbedding = new OllamaCustomTextEmbedding(ollamaUrl, embedModel);
            var store = SqliteMemoryStore.ConnectAsync(dbConnection).GetAwaiter().GetResult();
            
            return new SemanticTextMemory(store, customOllamaEmbedding);
#pragma warning restore CS0618
#pragma warning restore SKEXP0001
        });

        services.AddScoped(sp => 
        {
            var kernel = KernelFactory.CreateKernel(config);
            
            kernel.Plugins.AddFromObject(sp.GetRequiredService<RagPlugin>(), "RagPlugin");
            kernel.Plugins.AddFromObject(sp.GetRequiredService<SeoAutomationPlugin>(), "SeoAutomationPlugin");
            
            return kernel;
        });

        return services;
    }
}
