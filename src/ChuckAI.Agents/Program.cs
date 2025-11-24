using ChuckAI.Agents.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register MCP Tool Service as singleton
builder.Services.AddSingleton<McpToolService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<McpToolService>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var mcpToolService = new McpToolService(logger, configuration);

    // Initialize the MCP connection synchronously during startup
    mcpToolService.InitializeAsync().GetAwaiter().GetResult();

    return mcpToolService;
});

builder.Services.AddSingleton<ChuckNorrisAgentService>();
builder.Services.AddHttpClient();

builder.Build().Run();
