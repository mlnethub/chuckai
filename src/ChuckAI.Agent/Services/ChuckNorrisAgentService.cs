using Azure.Identity;
using ChuckAI.Core.DTOs;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace ChuckAI.Agent.Services;

public class ChuckNorrisAgentService
{
    private readonly ILogger<ChuckNorrisAgentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AIAgent _intentAgent;
    private readonly AIAgent _conversationAgent;
    private readonly ChatClient _chatClient;

    public ChuckNorrisAgentService(
        ILogger<ChuckNorrisAgentService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;

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

        // Create intent classification agent
        _intentAgent = _chatClient.CreateAIAgent(
            instructions: @"You are an intent classifier. Determine if the user wants information about Chuck Norris or has a general question.
Respond with ONLY one word:
- 'CHUCK_NORRIS' if the user wants Chuck Norris facts, jokes, or information
- 'GENERAL' for any other type of question or conversation",
            name: "IntentClassifier");

        // Create conversation agent for general questions
        _conversationAgent = _chatClient.CreateAIAgent(
            instructions: "You are a friendly and helpful AI assistant. Respond to user questions in a kind, warm, and helpful manner. Keep your responses concise but friendly.",
            name: "ConversationAssistant");
    }

    public async Task<string> ProcessUserMessage(string userMessage)
    {
        _logger.LogInformation("Processing user message: {Message}", userMessage);

        try
        {
            // Step 1: Determine user intent using the intent agent
            var intent = await DetermineIntent(userMessage);

            // Step 2: If intent is to get Chuck Norris info, call the API
            if (intent == UserIntent.GetChuckNorrisJoke)
            {
                var joke = await GetChuckNorrisJoke();
                if (joke != null)
                {
                    return $"Here's a Chuck Norris fact for you: {joke.Joke}";
                }
                return "I'm sorry, I couldn't fetch a Chuck Norris fact right now. Please try again later.";
            }

            // Step 3: For general questions, use the conversation agent
            return await GenerateKindResponse(userMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message");
            return "I apologize, but I encountered an error while processing your request. Please try again.";
        }
    }

    private async Task<UserIntent> DetermineIntent(string userMessage)
    {
        var response = await _intentAgent.RunAsync(userMessage);
        var intentText = response.ToString().Trim().ToUpperInvariant();

        _logger.LogInformation("Intent classification result: {Intent}", intentText);

        return intentText.Contains("CHUCK_NORRIS") || intentText.Contains("CHUCK")
            ? UserIntent.GetChuckNorrisJoke
            : UserIntent.General;
    }

    private async Task<JokeResponseDto?> GetChuckNorrisJoke()
    {
        try
        {
            var baseUrl = _configuration["ChuckAIApiBaseUrl"] ?? "http://localhost:7071";
            var httpClient = _httpClientFactory.CreateClient();

            _logger.LogInformation("Fetching joke from {BaseUrl}/api/jokes/random", baseUrl);

            var response = await httpClient.GetAsync($"{baseUrl}/api/jokes/random");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JokeResponseDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogWarning("Failed to fetch joke. Status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Chuck Norris joke from API");
            return null;
        }
    }

    private async Task<string> GenerateKindResponse(string userMessage)
    {
        var response = await _conversationAgent.RunAsync(userMessage);
        return response.ToString();
    }
}

public enum UserIntent
{
    GetChuckNorrisJoke,
    General
}
