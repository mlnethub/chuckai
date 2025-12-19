using ChuckAI.Core.DTOs;
using ChuckAI.Core.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Http; 
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

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
    public async Task<string> GetRandomJoke(
        [McpToolTrigger("get_random_joke", "Get a random joke about Chuck Norris from the database.")] ToolInvocationContext context)
    {
        _logger.LogInformation("Getting a random joke from the database.");

        try
        {
            var connectionString = _configuration["SqlConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("SQL connection string is not configured.");
                return "Database connection is not configured.";
            }

            using var connection = new NpgsqlConnection(connectionString);

            const string query = @"
                SELECT TOP 1 Id, Joke AS JokeText, CreatedAt, UpdatedAt
                FROM ChuckNorrisJokes
                ORDER BY NEWID()";

            var joke = await connection.QueryFirstOrDefaultAsync<Joke>(query);

            if (joke == null)
            {
                _logger.LogWarning("No jokes found in the database.");
                return "No jokes found in the database.";
                
            }

            var jokeResponse = new JokeResponseDto
            {
                Id = joke.Id,
                Joke = joke.JokeText,
                CreatedAt = joke.CreatedAt,
                UpdatedAt = joke.UpdatedAt
            };

            return JsonSerializer.Serialize(jokeResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching random joke.");
            return JsonSerializer.Serialize(new { error = "An error occurred while processing your request." });
        }
    }

    [Function("SaveJoke")]
    public async Task<string> SaveJoke(
        [McpToolTrigger("save_new_joke", "Save a new joke about Chuck Norris to the database.")] ToolInvocationContext context,
        [McpToolProperty("joke", "The joke text to save.", isRequired: true)]
        string joke)
    {
        _logger.LogInformation("Saving a new joke to the database.");

        try
        {
            var connectionString = _configuration["SqlConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("SQL connection string is not configured.");
                return "Database connection is not configured.";
                  
            }

            
            using var connection = new NpgsqlConnection(connectionString);

            const string query = @"
                INSERT INTO ChuckNorrisJokes (Joke, CreatedAt, UpdatedAt)
                VALUES (@Joke, @CreatedAt, @UpdatedAt)";

            var now = DateTime.UtcNow;

            var insertedJoke = await connection.ExecuteAsync(query, new
            {
                Joke = joke,
                CreatedAt = now,
                UpdatedAt = now
            });

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving joke.");
            return "An error occurred while processing your request.";  
        }
    }
}

