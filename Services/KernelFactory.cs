using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

namespace AI_SEO_Ssas_Platform.Services;

public static class KernelFactory
{
    public static Kernel CreateKernel(IConfiguration config)
    {
        var builder = Kernel.CreateBuilder();
        
        string endpoint = config["AI:Endpoint"] ?? "http://localhost:11434/v1";
        string modelId = config["AI:ModelId"] ?? "llama3.2";
        string apiKey = config["AI:ApiKey"] ?? "ollama_key_dummy";

        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            endpoint: new Uri(endpoint)
        );
        
        return builder.Build();
    }
}
