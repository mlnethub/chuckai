# ChuckAI - Demo MCP con Azure Functions

Progetto demo per WPC 2025 - sessione: **"Serverless MCP: il tuo remote server con Azure Function MCP extension"**

Questo progetto dimostra come costruire un'applicazione AI-powered serverless utilizzando il Model Context Protocol (MCP) con Azure Functions, mostrando come gli agenti AI possono interagire dinamicamente con strumenti esposti attraverso server MCP.

## Panoramica

ChuckAI è un chatbot di battute su Chuck Norris che dimostra l'integrazione tra:
- **Azure Functions** come server MCP e agenti AI
- **Model Context Protocol (MCP)** per la scoperta e l'invocazione di strumenti
- **Azure AI Foundry** per la gestione intelligente delle conversazioni
- **Blazor Web App** per l'interfaccia utente

### Architettura

```
┌─────────────────┐
│   ChuckAI.Web   │  Blazor Web App (Interfaccia Utente)
│   (Porta: 5000) │
└────────┬────────┘
         │ HTTP
         ▼
┌─────────────────┐
│ ChuckAI.Agents  │  Azure Functions - Servizio Agente AI
│   (Porta: 7072) │  • Ospita l'agente AI conversazionale
│                 │  • Si connette al server MCP
└────────┬────────┘  • Utilizza Azure OpenAI per l'intelligenza
         │
         │ Protocollo MCP
         ▼
┌─────────────────┐
│ChuckAI.Database │  Azure Functions - Server MCP
│      .Api       │  • Espone strumenti MCP via HTTP
│   (Porta: 7071) │  • Tool: get_random_joke
└────────┬────────┘  • Tool: save_new_joke
         │
         │ SQL
         ▼
┌─────────────────┐
│  Azure SQL DB   │  Database con le battute di Chuck Norris
└─────────────────┘
```

## Caratteristiche Principali

1. **Server MCP (Database.Api)**
   - Espone operazioni sul database come strumenti MCP
   - Gli strumenti vengono scoperti automaticamente dall'agente AI
   - Utilizza l'estensione MCP di Azure Functions (`Microsoft.Azure.Functions.Worker.Extensions.Mcp`)

2. **Agente AI (Agents)**
   - Si connette al server MCP all'avvio
   - Chiama automaticamente gli strumenti appropriati in base all'intento dell'utente
   - Non è necessaria il rilevamento manuale dell'intento o chiamate API
   - Utilizza Azure AI Foundry (OpenAI) per la comprensione del linguaggio naturale

3. **Interfaccia Web (Web)**
   - Interfaccia chat basata su Blazor
   - Comunica con il servizio agente AI

## Prerequisiti

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure SQL Database](https://azure.microsoft.com/services/sql-database/) o SQL Server
- Account [Azure AI Foundry](https://azure.microsoft.com/products/ai-foundry/) con deployment OpenAI

## Configurazione per lo Sviluppo Locale

### 1. Setup del Database

Esegui lo script del database per creare lo schema e i dati iniziali:

```bash
# Connettiti al tuo SQL Server/Azure SQL Database ed esegui:
sqlcmd -S your-server.database.windows.net -U your-username -P your-password -i database/Script\ Database.sql
```

### 2. Configurazione di ChuckAI.Database.Api (Server MCP)

Crea o aggiorna `src/ChuckAI.Database.Api/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=ChuckNorrisJokes;User ID=your-username;Password=your-password;Encrypt=True;"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*"
  }
}
```

### 3. Configurazione di ChuckAI.Agents (Servizio Agente AI)

Crea o aggiorna `src/ChuckAI.Agents/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureAIFoundryEndpoint": "https://your-instance.openai.azure.com/openai/v1/",
    "AzureAIFoundryKey": "your-azure-openai-api-key",
    "AzureAIFoundryModelName": "gpt-4o",
    "McpServerUrl": "http://127.0.0.1:7071/runtime/webhooks/mcp"
  }
}
```

**Note Importanti:**
- Usa `127.0.0.1` invece di `localhost` per l'URL del server MCP per evitare problemi di connessione IPv6
- `McpServerUrl` punta all'endpoint MCP esposto dal servizio Database.Api

### 4. Avvio dell'Applicazione

Apri **tre finestre del terminale** ed esegui i seguenti comandi:

#### Terminale 1 - Avvia il Server MCP (Database.Api)
```bash
cd src/ChuckAI.Database.Api
func start --port 7071
```

Dovresti vedere:
```
MCP tools configured: get_random_joke, save_new_joke
Functions:
    GetRandomJoke: [McpToolTrigger] http://localhost:7071/runtime/webhooks/mcp
    SaveJoke: [McpToolTrigger] http://localhost:7071/runtime/webhooks/mcp
```

#### Terminale 2 - Avvia il Servizio Agente AI
```bash
cd src/ChuckAI.Agents
func start --port 7072
```

L'agente si connetterà al server MCP all'avvio e caricherà gli strumenti disponibili.

#### Terminale 3 - Avvia la Web App
```bash
cd src/ChuckAI.Web
dotnet run
```

Accedi alla web app su: `http://localhost:5000`

## Test del Server MCP

Puoi testare il server MCP usando [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
npx @modelcontextprotocol/inspector
```

Nell'inspector:
1. Seleziona il trasporto **"HTTP (StreamableHTTP)"**
2. Inserisci l'URL: `http://127.0.0.1:7071/runtime/webhooks/mcp`
3. Clicca su **Connect**

Dovresti vedere gli strumenti disponibili:
- `get_random_joke` - Recupera una battuta casuale su Chuck Norris dal database
- `save_new_joke` - Salva una nuova battuta nel database (richiede il parametro joke)

## Test dell'API Chat

Puoi testare l'agente direttamente usando curl:

```bash
# Chiedi una battuta su Chuck Norris
curl -X POST http://localhost:7072/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Dimmi una battuta su Chuck Norris"}'

# Conversazione generica
curl -X POST http://localhost:7072/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Come va oggi?"}'
```

## Come Funziona

### Approccio Tradizionale (Prima di MCP)
1. L'utente invia un messaggio → L'agente rileva l'intento → L'agente fa una chiamata HTTP all'API → Restituisce la risposta
2. Richiesta classificazione manuale dell'intento
3. Endpoint API hard-coded
4. Accoppiamento stretto tra agente e API

### Approccio MCP (Implementazione Attuale)
1. L'utente invia un messaggio → **L'agente scopre automaticamente e chiama lo strumento MCP appropriato** → Restituisce la risposta
2. **Nessun rilevamento manuale dell'intento** - il LLM decide quale strumento usare
3. **Scoperta dinamica degli strumenti** dal server MCP
4. **Accoppiamento lasso** - l'agente conosce solo il protocollo MCP, non le API specifiche

### Punti Salienti del Codice

#### Server MCP (Database.Api/Functions/JokesFunction.cs)
```csharp
[Function("GetRandomJoke")]
public async Task<string> GetRandomJoke(
    [McpToolTrigger("get_random_joke", "Get a random joke about Chuck Norris from the database.")]
    ToolInvocationContext context)
{
    // Implementazione dello strumento
}
```

#### Agente AI con Strumenti MCP (Agents/Services/ChuckNorrisAgentService.cs)
```csharp
// L'agente viene creato con gli strumenti MCP
_agent = _chatClient.CreateAIAgent(
    instructions: "Sei un assistente AI amichevole con accesso al database di battute su Chuck Norris...",
    name: "ChuckNorrisAssistant",
    tools: _mcpToolService.GetTools());  // ← Strumenti MCP caricati automaticamente

// L'agente decide automaticamente quando chiamare gli strumenti
var response = await _agent.RunAsync(userMessage);
```

## Struttura del Progetto

```
chuckai/
├── database/
│   └── Script Database.sql          # Schema database e dati iniziali
├── src/
│   ├── ChuckAI.Core/               # DTO e modelli condivisi
│   ├── ChuckAI.Database.Api/       # Server MCP (Azure Functions)
│   │   ├── Functions/
│   │   │   └── JokesFunction.cs    # Implementazioni tool MCP
│   │   └── Program.cs              # Registrazione tool MCP
│   ├── ChuckAI.Agents/             # Servizio Agente AI (Azure Functions)
│   │   ├── Functions/
│   │   │   └── ChatFunction.cs     # Endpoint API chat
│   │   ├── Services/
│   │   │   ├── ChuckNorrisAgentService.cs  # Agente AI con tool MCP
│   │   │   └── McpToolService.cs           # Connettore client MCP
│   │   └── Program.cs              # Inizializzazione client MCP
│   └── ChuckAI.Web/                # Blazor Web App
└── README.md
```

## Dipendenze Principali

- **Microsoft.Azure.Functions.Worker.Extensions.Mcp** - Supporto server MCP per Azure Functions
- **ModelContextProtocol** - SDK client MCP
- **Microsoft.Agents.AI** - Framework agenti AI
- **Microsoft.Agents.AI.OpenAI** - Integrazione OpenAI per agenti

## Risoluzione Problemi

### Problemi di Connessione MCP

**Problema:** `ECONNREFUSED ::1:7071`

**Soluzione:** Usa `127.0.0.1` invece di `localhost` nella configurazione `McpServerUrl`. Windows potrebbe preferire IPv6 (`::1`) per `localhost`, ma Azure Functions ascolta su IPv4 per impostazione predefinita.

### L'Agente Non Chiama gli Strumenti

**Problema:** L'agente risponde senza chiamare gli strumenti MCP

**Soluzione:**
- Verifica che il server MCP sia in esecuzione sulla porta 7071
- Controlla i log dell'agente per errori di connessione MCP
- Assicurati che gli strumenti siano caricati all'avvio (cerca "Loaded N tools from MCP server" nei log)

### Problemi di Connessione al Database

**Problema:** Impossibile connettersi ad Azure SQL Database

**Soluzione:**
- Verifica che il tuo indirizzo IP sia nella whitelist delle regole firewall di Azure SQL
- Testa la stringa di connessione con SQL Server Management Studio
- Assicurati che le credenziali in `local.settings.json` siano corrette

## Approfondimenti

- [Documentazione Model Context Protocol](https://modelcontextprotocol.io/)
- [Estensione MCP per Azure Functions](https://learn.microsoft.com/azure/azure-functions/)
- [Documentazione Microsoft Agents AI](https://github.com/microsoft/agents)
- [Azure AI Foundry](https://azure.microsoft.com/products/ai-foundry/)

## Licenza

Questo è un progetto demo per scopi educativi.

## Autore

Presentato a WPC 2025 - "Serverless MCP: il tuo remote server con Azure Function MCP extension"
