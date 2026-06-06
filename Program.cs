using AI_SEO_Ssas_Platform.Services;
using AI_SEO_Ssas_Platform.Extensions;
using AI_SEO_Ssas_Platform.Endpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.SemanticKernel.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowAll");

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

using (var scope = app.Services.CreateScope())
{
    var memory = scope.ServiceProvider.GetRequiredService<ISemanticTextMemory>();
    await DataSeeder.SeedAsync(memory);
}

app.MapHub<AgentHub>("/agentHub");
app.MapAgentEndpoints();

app.Run();
