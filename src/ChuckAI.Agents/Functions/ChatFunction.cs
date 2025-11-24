using ChuckAI.Agents.Services;
using ChuckAI.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ChuckAI.Agents.Functions;

public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    private readonly ChuckNorrisAgentService _agentService;

    public ChatFunction(ILogger<ChatFunction> logger, ChuckNorrisAgentService agentService)
    {
        _logger = logger;
        _agentService = agentService;
    }

    [Function("Chat")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequestData req)
    {
        _logger.LogInformation("Chat endpoint invoked.");

        try
        {
            var requestBody = await req.ReadFromJsonAsync<ChatMessageDto>();

            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Message))
            {
                _logger.LogWarning("Invalid request body. Message is required.");
                return new BadRequestObjectResult(new { error = "Message is required." });
            }

            _logger.LogInformation("Processing message: {Message}", requestBody.Message);

            var agentResponse = await _agentService.ProcessUserMessage(requestBody.Message);

            var chatResponse = new ChatResponseDto
            {
                Response = agentResponse
            };

            return new OkObjectResult(chatResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing chat message.");
            return new ObjectResult(new { error = "An error occurred while processing your request." })
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}
