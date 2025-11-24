using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace ChuckAI.Agents.Services;

public class ChuckNorrisAgentService
{
    private readonly ILogger<ChuckNorrisAgentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly McpToolService _mcpToolService;
    private readonly AIAgent _agent;
    private readonly ChatClient _chatClient;

    public ChuckNorrisAgentService(
        ILogger<ChuckNorrisAgentService> logger,
        IConfiguration configuration,
        McpToolService mcpToolService)
    {
        _logger = logger;
        _configuration = configuration;
        _mcpToolService = mcpToolService;

        var endpoint = _configuration["AzureAIFoundryEndpoint"] ?? throw new InvalidOperationException("AzureAIFoundryEndpoint not configured");
        var apiKey = _configuration["AzureAIFoundryKey"];
        var modelName = _configuration["AzureAIFoundryModelName"] ?? "gpt-4o";

        // Create OpenAI client with Azure AI Foundry endpoint
        OpenAIClient openAIClient;
        if (!string.IsNullOrEmpty(apiKey))
        {
            openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            });
        }
        else
        {
            // Use Azure CLI credentials if no API key provided
            var credential = new AzureCliCredential();
            var token = credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }), CancellationToken.None).Result;
            openAIClient = new OpenAIClient(new ApiKeyCredential(token.Token), new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            });
        }

        _chatClient = openAIClient.GetChatClient(modelName);

        // Create agent with MCP tools
        _agent = _chatClient.CreateAIAgent(
            instructions: @"You are a friendly and helpful AI assistant with access to Chuck Norris jokes database.
When users ask about Chuck Norris or want to hear jokes, use the get_random_joke tool to fetch a joke from the database.
If users want to save a new joke, use the save_new_joke tool.
For general conversation, respond in a kind, warm, and helpful manner.
Keep your responses concise but friendly.",
            name: "ChuckNorrisAssistant",
            tools: _mcpToolService.GetTools());

        _logger.LogInformation("ChuckNorrisAgentService initialized with {ToolCount} MCP tools", _mcpToolService.GetTools().Count);
    }

    public async Task<string> ProcessUserMessage(string userMessage)
    {
        _logger.LogInformation("Processing user message: {Message}", userMessage);

        try
        {
            // Let the agent handle the message and decide if tools are needed
            var response = await _agent.RunAsync(userMessage);
            return response.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message");
            return "I apologize, but I encountered an error while processing your request. Please try again.";
        }
    }
}
