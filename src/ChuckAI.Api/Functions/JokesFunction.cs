using ChuckAI.Core.DTOs;
using ChuckAI.Core.Models;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ChuckAI.Api.Functions;

public class JokesFunction
{
    private readonly ILogger<JokesFunction> _logger;
    private readonly IConfiguration _configuration;

    public JokesFunction(ILogger<JokesFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function("GetRandomJoke")]
    public async Task<HttpResponseData> GetRandomJoke(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "jokes/random")] HttpRequestData req)
    {
        _logger.LogInformation("Getting a random joke from the database.");

        try
        {
            var connectionString = _configuration["SqlConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("SQL connection string is not configured.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Database connection is not configured." });
                return errorResponse;
            }

            using var connection = new SqlConnection(connectionString);

            const string query = @"
                SELECT TOP 1 Id, Joke AS JokeText, Url, CreatedAt, UpdatedAt
                FROM ChuckNorrisJokes
                ORDER BY NEWID()";

            var joke = await connection.QueryFirstOrDefaultAsync<Joke>(query);

            if (joke == null)
            {
                _logger.LogWarning("No jokes found in the database.");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "No jokes found in the database." });
                return notFoundResponse;
            }

            var jokeResponse = new JokeResponseDto
            {
                Id = joke.Id,
                Joke = joke.JokeText,
                Url = joke.Url,
                CreatedAt = joke.CreatedAt,
                UpdatedAt = joke.UpdatedAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(jokeResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching random joke.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "An error occurred while processing your request." });
            return errorResponse;
        }
    }

    [Function("SaveJoke")]
    public async Task<HttpResponseData> SaveJoke(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jokes")] HttpRequestData req)
    {
        _logger.LogInformation("Saving a new joke to the database.");

        try
        {
            var connectionString = _configuration["SqlConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("SQL connection string is not configured.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Database connection is not configured." });
                return errorResponse;
            }

            var requestBody = await req.ReadFromJsonAsync<CreateJokeRequestDto>();

            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Joke))
            {
                _logger.LogWarning("Invalid request body. Joke text is required.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { error = "Joke text is required." });
                return badRequestResponse;
            }

            using var connection = new SqlConnection(connectionString);

            const string query = @"
                INSERT INTO ChuckNorrisJokes (Joke, Url, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id, INSERTED.Joke, INSERTED.Url, INSERTED.CreatedAt, INSERTED.UpdatedAt
                VALUES (@Joke, @Url, @CreatedAt, @UpdatedAt)";

            var now = DateTime.UtcNow;

            var insertedJoke = await connection.QuerySingleAsync<dynamic>(query, new
            {
                Joke = requestBody.Joke,
                Url = requestBody.Url,
                CreatedAt = now,
                UpdatedAt = now
            });

            var jokeResponse = new JokeResponseDto
            {
                Id = insertedJoke.Id,
                Joke = insertedJoke.Joke,
                Url = insertedJoke.Url,
                CreatedAt = insertedJoke.CreatedAt,
                UpdatedAt = insertedJoke.UpdatedAt
            };

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(jokeResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving joke.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "An error occurred while processing your request." });
            return errorResponse;
        }
    }
}
