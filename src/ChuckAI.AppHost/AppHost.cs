var builder = DistributedApplication.CreateBuilder(args);

// Add the MCP Database API (Azure Functions)
var databaseApi = builder.AddProject<Projects.ChuckAI_Database_Api>("chuckai-database-api")
    .WithHttpEndpoint(port: 7071, name: "http");

// Add the Agents service (Azure Functions)
var agents = builder.AddProject<Projects.ChuckAI_Agents>("chuckai-agents")
    .WithHttpEndpoint(port: 7072, name: "http")
    .WithReference(databaseApi)
    .WaitFor(databaseApi);

// Add the Web frontend (Blazor)
var web = builder.AddProject<Projects.ChuckAI_Web>("chuckai-web")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithReference(agents)
    .WaitFor(agents);

builder.Build().Run();
