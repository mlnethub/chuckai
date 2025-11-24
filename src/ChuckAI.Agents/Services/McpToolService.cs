using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace ChuckAI.Agents.Services;

public class McpToolService : IAsyncDisposable
{
    private readonly ILogger<McpToolService> _logger;
    private readonly IConfiguration _configuration;
    private McpClient? _mcpClient;
    private IList<McpClientTool>? _tools;

    public McpToolService(ILogger<McpToolService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        var mcpServerUrl = _configuration["McpServerUrl"] ?? "http://127.0.0.1:7071/runtime/webhooks/mcp";

        _logger.LogInformation("Connecting to MCP server at {Url}", mcpServerUrl);

        try
        {
            // Create MCP client with HTTP transport
            var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(mcpServerUrl)
            });
            _mcpClient = await McpClient.CreateAsync(clientTransport);

            _logger.LogInformation("Successfully connected to MCP server");

            // Load available tools from the MCP server
            await LoadToolsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MCP server at {Url}", mcpServerUrl);
            throw;
        }
    }

    private async Task LoadToolsAsync()
    {
        if (_mcpClient == null)
        {
            throw new InvalidOperationException("MCP client is not initialized");
        }

        try
        {
            _tools = await _mcpClient.ListToolsAsync();

            _logger.LogInformation("Loaded {Count} tools from MCP server", _tools.Count);

            foreach (var tool in _tools)
            {
                _logger.LogInformation("Loaded MCP tool: {ToolName}", tool.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tools from MCP server");
            throw;
        }
    }

    public IList<AITool> GetTools()
    {
        if (_tools == null)
        {
            throw new InvalidOperationException("Tools not loaded. Call InitializeAsync first.");
        }

        // McpClientTool inherits from AIFunction, so we can cast it to AITool
        return _tools.Cast<AITool>().ToList();
    }

    public async ValueTask DisposeAsync()
    {
        if (_mcpClient != null)
        {
            await _mcpClient.DisposeAsync();
        }
    }
}
