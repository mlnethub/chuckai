using Aspire.Hosting;
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// Add a parameter
var pgUsername = builder.AddParameter("PostgresUsername");
var pgPassword = builder.AddParameter("PostgresPassword", secret: true);

var postgresServer = builder
    .AddPostgres("postgresqlServer", pgUsername, pgPassword, port: 5432)
    .WithImageTag("18-alpine3.22")
    .WithDataVolume("postgres18_data")
    .WithPgAdmin(
       c => c.WithImage("dpage/pgadmin4")
             .WithImageTag("9.10")
             .WithHostPort(5050)
    );

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
// Add the MCP Database API (Azure Functions)
var databaseApi = builder.AddAzureFunctionsProject<Projects.ChuckAI_Database_Api>("chuckai-database-api")
    .WithHostStorage(storage)
    .WithReference(postgresServer)
    //.WithHttpEndpoint(port: 7071, name: "database-http")
    .WaitFor(postgresServer);

// Add the Agents service (Azure Functions)
var agents = builder.AddAzureFunctionsProject<Projects.ChuckAI_Agents>("chuckai-agents")
    .WithHostStorage(storage)
    //.WithHttpEndpoint(port: 7072, name: "agents-http")
    .WithReference(databaseApi)
    .WaitFor(databaseApi);

// Add the Web frontend (Blazor)
var web = builder.AddProject<Projects.ChuckAI_Web>("chuckai-web")
    //.WithHttpEndpoint(port: 5000, name: "web-http")
    .WithReference(agents)
    .WaitFor(agents);

builder.Build().Run();
