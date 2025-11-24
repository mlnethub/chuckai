using ChuckAI.Agent.Services;
using ChuckAI.Core.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ChuckAI.Agent.Functions;

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
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequestData req)
    {
        _logger.LogInformation("Chat endpoint invoked.");

        try
        {
            var requestBody = await req.ReadFromJsonAsync<ChatMessageDto>();

            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Message))
            {
                _logger.LogWarning("Invalid request body. Message is required.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { error = "Message is required." });
                return badRequestResponse;
            }

            _logger.LogInformation("Processing message: {Message}", requestBody.Message);

            var agentResponse = await _agentService.ProcessUserMessage(requestBody.Message);

            var chatResponse = new ChatResponseDto
            {
                Response = agentResponse
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(chatResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing chat message.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "An error occurred while processing your request." });
            return errorResponse;
        }
    }
}
