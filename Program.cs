using AI_SEO_Ssas_Platform.Services;
using AI_SEO_Ssas_Platform.Plugins;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Sqlite;
var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite default port
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 2. Đăng ký SignalR
builder.Services.AddSignalR();

// 3. Đăng ký Services & Plugins
builder.Services.AddScoped<ILogCollector, LogCollector>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
builder.Services.AddScoped<RagPlugin>();
builder.Services.AddScoped<SeoAutomationPlugin>();

// Đăng ký ISemanticTextMemory (Vector Database)
builder.Services.AddSingleton<ISemanticTextMemory>(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
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

// 4. Đăng ký Semantic Kernel qua DI
builder.Services.AddScoped(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    var kernel = KernelFactory.CreateKernel(config);
    
    // Thêm các Plugin vào Kernel qua DI container
    kernel.Plugins.AddFromObject(sp.GetRequiredService<RagPlugin>(), "RagPlugin");
    kernel.Plugins.AddFromObject(sp.GetRequiredService<SeoAutomationPlugin>(), "SeoAutomationPlugin");
    
    return kernel;
});

var app = builder.Build();

app.UseCors("AllowAll");

// Global Exception Handler
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var errorMsg = exceptionHandlerPathFeature?.Error.Message ?? "Lỗi hệ thống không xác định.";

        await context.Response.WriteAsJsonAsync(new { Error = $"[LỖI HỆ THỐNG] {errorMsg}" });
    });
});

// Khởi tạo Database khi Start
using (var scope = app.Services.CreateScope())
{
    var memory = scope.ServiceProvider.GetRequiredService<ISemanticTextMemory>();
    await DataSeeder.SeedAsync(memory);
}

// Map Hubs
app.MapHub<AgentHub>("/agentHub");

// Endpoints
app.MapGet("/", () => "AI SEO Agent API is running with SignalR support!");

app.MapPost("/api/agent/run", async (RunRequest request, IAgentOrchestrator orchestrator) =>
{
    var result = await orchestrator.RunAgentAsync(request.Input, request.ConnectionId);
    return Results.Ok(result);
});

app.Run();

public record RunRequest(string Input, string ConnectionId);
