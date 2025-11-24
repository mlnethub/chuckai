using ChuckAI.Core.DTOs;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ChuckAI.Web.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(HttpClient httpClient, ILogger<ChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync(string message)
    {
        try
        {
            var request = new ChatMessageDto { Message = message };

            // Manually serialize the object
            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending message to agent: {Message}", message);
            _logger.LogInformation("JSON payload: {Json}", jsonContent);

            var response = await _httpClient.PostAsync("/api/chat", content);

            _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response content: {Content}", responseContent);

                var chatResponse = JsonSerializer.Deserialize<ChatResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return chatResponse?.Response ?? "No response received.";
            }

            _logger.LogWarning("Failed to get response from agent. Status code: {StatusCode}", response.StatusCode);
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Error response: {ErrorContent}", errorContent);
            return "Sorry, I couldn't process your request at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to agent");
            return "Sorry, I encountered an error. Please try again.";
        }
    }
}
