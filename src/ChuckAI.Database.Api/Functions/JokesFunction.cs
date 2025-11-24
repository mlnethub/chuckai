using ChuckAI.Core.DTOs;
using ChuckAI.Core.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ChuckAI.Database.Api.Functions;

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
    public async Task<IActionResult> GetRandomJoke(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "jokes/random")] HttpRequestData req)
    {
        _logger.LogInformation("Getting a random joke from the database.");

        try
        {
            var connectionString = _configuration["SqlConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("SQL connection string is not configured.");
                return new ObjectResult(new { error = "Database connection is not configured." })
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            using var connection = new SqlConnection(connectionString);

            const string query = @"
                SELECT TOP 1 Id, Joke AS JokeText, CreatedAt, UpdatedAt
                FROM ChuckNorrisJokes
                ORDER BY NEWID()";

            var joke = await connection.QueryFirstOrDefaultAsync<Joke>(query);

            if (joke == null)
            {
                _logger.LogWarning("No jokes found in the database.");
                return new NotFoundObjectResult(new { error = "No jokes found in the database." });
                
            }

            var jokeResponse = new JokeResponseDto
            {
                Id = joke.Id,
                Joke = joke.JokeText,
                CreatedAt = joke.CreatedAt,
                UpdatedAt = joke.UpdatedAt
            };

            return new OkObjectResult(jokeResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching random joke.");
            return new ObjectResult(new { error = "An error occurred while processing your request." })
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }

    [Function("SaveJoke")]
    public async Task<IActionResult> SaveJoke(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jokes")] HttpRequestData req)
    {
        _logger.LogInformation("Saving a new joke to the database.");

        try
        {
            var connectionString = _configuration["SqlConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("SQL connection string is not configured.");
                return new ObjectResult(new { error = "Database connection is not configured." })
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            var requestBody = await req.ReadFromJsonAsync<CreateJokeRequestDto>();

            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Joke))
            {
                _logger.LogWarning("Invalid request body. Joke text is required.");
                return new BadRequestObjectResult(new { error = "Joke text is required." });
            }

            using var connection = new SqlConnection(connectionString);

            const string query = @"
                INSERT INTO ChuckNorrisJokes (Joke, CreatedAt, UpdatedAt)
                VALUES (@Joke, @CreatedAt, @UpdatedAt)";

            var now = DateTime.UtcNow;

            var insertedJoke = await connection.ExecuteAsync(query, new
            {
                Joke = requestBody.Joke,
                CreatedAt = now,
                UpdatedAt = now
            });

            return new CreatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving joke.");
            return new ObjectResult(new { error = "An error occurred while processing your request." })
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}

