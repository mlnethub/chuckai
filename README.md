# ChuckAI - Azure Functions MCP 演示项目

WPC 2025 演示项目 - 会议主题：**"无服务器 MCP：使用 Azure Function MCP 扩展的远程服务器"**

本项目演示如何使用 Model Context Protocol (MCP) 和 Azure Functions 构建 AI 驱动的无服务器应用程序，展示 AI 代理如何通过 MCP 服务器动态地与公开的工具进行交互。

## 概述

ChuckAI 是一个关于 Chuck Norris 笑话的聊天机器人，演示了以下技术的集成：
- **Azure Functions** 作为 MCP 服务器和 AI 代理
- **Model Context Protocol (MCP)** 用于工具的发现和调用
- **Azure AI Foundry** 用于智能对话管理
- **Blazor Web App** 作为用户界面
- **.NET Aspire** 用于编排和可观测性

### 架构

```
┌─────────────────┐
│   ChuckAI.Web   │  Blazor Web App (用户界面)
│   (端口: 5000)  │
└────────┬────────┘
         │ HTTP
         ▼
┌─────────────────┐
│ ChuckAI.Agents  │  Azure Functions - AI 代理服务
│   (端口: 7072)  │  • 托管对话式 AI 代理
│                 │  • 连接到 MCP 服务器
└────────┬────────┘  • 使用 Azure OpenAI 进行智能处理
         │
         │ MCP 协议
         ▼
┌─────────────────┐
│ChuckAI.Database │  Azure Functions - MCP 服务器
│      .Api       │  • 通过 HTTP 公开 MCP 工具
│   (端口: 7071)  │  • 工具: get_random_joke
└────────┬────────┘  • 工具: save_new_joke
         │
         │ SQL
         ▼
┌─────────────────┐
│  Azure SQL DB   │  Chuck Norris 笑话数据库
└─────────────────┘
```

## 主要特性

1. **MCP 服务器 (Database.Api)**
   - 将数据库操作公开为 MCP 工具
   - 工具会被 AI 代理自动发现
   - 使用 Azure Functions 的 MCP 扩展 (`Microsoft.Azure.Functions.Worker.Extensions.Mcp`)

2. **AI 代理 (Agents)**
   - 启动时连接到 MCP 服务器
   - 根据用户意图自动调用适当的工具
   - 无需手动检测意图或 API 调用
   - 使用 Azure AI Foundry (OpenAI) 进行自然语言理解

3. **Web 界面 (Web)**
   - 基于 Blazor 的聊天界面
   - 与 AI 代理服务通信

## 前置要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure SQL Database](https://azure.microsoft.com/services/sql-database/) 或 SQL Server
- 带有 OpenAI 部署的 [Azure AI Foundry](https://azure.microsoft.com/products/ai-foundry/) 账户

## 使用 .NET Aspire 启动应用程序（推荐）

.NET Aspire 现已集成到项目中！Aspire 为所有应用程序提供本地编排、内置可观测性和服务发现。

### .NET Aspire 的优势

- **简化启动**：一个命令即可启动所有服务
- **集成仪表板**：可视化所有服务的日志、指标和跟踪
- **服务发现**：服务之间自动相互发现
- **健康检查**：监控每个服务的状态
- **OpenTelemetry**：开箱即用的分布式跟踪和指标

### 数据库设置

在启动应用程序之前，运行数据库脚本以创建架构和初始数据：

```bash
# 连接到你的 SQL Server/Azure SQL Database 并执行：
sqlcmd -S your-server.database.windows.net -U your-username -P your-password -i database/Script\ Database.sql
```

### 配置

为 Azure Functions 服务配置 `local.settings.json` 文件：

**`src/ChuckAI.Database.Api/local.settings.json`:**
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

**`src/ChuckAI.Agents/local.settings.json`:**
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

### 使用 Aspire 启动

运行 AppHost 以自动启动所有服务：

```bash
cd src/ChuckAI.AppHost
dotnet run
```

Aspire 会在浏览器中自动打开仪表板。在那里你可以：
- 查看所有正在运行的服务
- 访问每个服务的日志
- 可视化指标和跟踪
- 访问 Web 应用程序

Web 应用程序将通过 Aspire 仪表板中的链接访问（通常是 `http://localhost:5000`）。

## 本地开发手动配置

### 1. 数据库设置

运行数据库脚本以创建架构和初始数据：

```bash
# 连接到你的 SQL Server/Azure SQL Database 并执行：
sqlcmd -S your-server.database.windows.net -U your-username -P your-password -i database/Script\ Database.sql
```

### 2. 配置 ChuckAI.Database.Api (MCP 服务器)

创建或更新 `src/ChuckAI.Database.Api/local.settings.json`：

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

### 3. 配置 ChuckAI.Agents (AI 代理服务)

创建或更新 `src/ChuckAI.Agents/local.settings.json`：

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

**重要提示:**
- 使用 `127.0.0.1` 而不是 `localhost` 作为 MCP 服务器 URL，以避免 IPv6 连接问题
- `McpServerUrl` 指向 Database.Api 服务公开的 MCP 端点

### 4. 启动应用程序

打开**三个终端窗口**并运行以下命令：

#### 终端 1 - 启动 MCP 服务器 (Database.Api)
```bash
cd src/ChuckAI.Database.Api
func start --port 7071
```

你应该看到：
```
MCP tools configured: get_random_joke, save_new_joke
Functions:
    GetRandomJoke: [McpToolTrigger] http://localhost:7071/runtime/webhooks/mcp
    SaveJoke: [McpToolTrigger] http://localhost:7071/runtime/webhooks/mcp
```

#### 终端 2 - 启动 AI 代理服务
```bash
cd src/ChuckAI.Agents
func start --port 7072
```

代理将在启动时连接到 MCP 服务器并加载可用的工具。

#### 终端 3 - 启动 Web 应用
```bash
cd src/ChuckAI.Web
dotnet run
```

访问 Web 应用：`http://localhost:5000`

## 测试 MCP 服务器

你可以使用 [MCP Inspector](https://github.com/modelcontextprotocol/inspector) 测试 MCP 服务器：

```bash
npx @modelcontextprotocol/inspector
```

在 inspector 中：
1. 选择 **"HTTP (StreamableHTTP)"** 传输方式
2. 输入 URL：`http://127.0.0.1:7071/runtime/webhooks/mcp`
3. 点击 **Connect**

你应该能看到可用的工具：
- `get_random_joke` - 从数据库中检索一个随机的 Chuck Norris 笑话
- `save_new_joke` - 保存一个新笑话到数据库（需要 joke 参数）

## 测试聊天 API

你可以使用 curl 直接测试代理：

```bash
# 请求一个 Chuck Norris 笑话
curl -X POST http://localhost:7072/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "告诉我一个关于 Chuck Norris 的笑话"}'

# 普通对话
curl -X POST http://localhost:7072/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "今天怎么样？"}'
```

## 工作原理

### 传统方法（MCP 之前）
1. 用户发送消息 → 代理检测意图 → 代理调用 HTTP API → 返回响应
2. 需要手动进行意图分类
3. API 端点硬编码
4. 代理与 API 之间紧密耦合

### MCP 方法（当前实现）
1. 用户发送消息 → **代理自动发现并调用适当的 MCP 工具** → 返回响应
2. **无需手动检测意图** - LLM 决定使用哪个工具
3. **动态发现工具** 从 MCP 服务器
4. **松散耦合** - 代理只知道 MCP 协议，不了解特定的 API

### 代码要点

#### MCP 服务器 (Database.Api/Functions/JokesFunction.cs)
```csharp
[Function("GetRandomJoke")]
public async Task<string> GetRandomJoke(
    [McpToolTrigger("get_random_joke", "Get a random joke about Chuck Norris from the database.")]
    ToolInvocationContext context)
{
    // 工具实现
}
```

#### 带 MCP 工具的 AI 代理 (Agents/Services/ChuckNorrisAgentService.cs)
```csharp
// 代理使用 MCP 工具创建
_agent = _chatClient.CreateAIAgent(
    instructions: "你是一个友好的 AI 助手，可以访问 Chuck Norris 笑话数据库...",
    name: "ChuckNorrisAssistant",
    tools: _mcpToolService.GetTools());  // ← 自动加载 MCP 工具

// 代理自动决定何时调用工具
var response = await _agent.RunAsync(userMessage);
```

## 项目结构

```
chuckai/
├── database/
│   └── Script Database.sql          # 数据库架构和初始数据
├── src/
│   ├── ChuckAI.AppHost/            # .NET Aspire AppHost (编排器)
│   │   └── AppHost.cs              # Aspire 服务配置
│   ├── ChuckAI.ServiceDefaults/    # Aspire 共享配置
│   │   └── Extensions.cs           # 服务发现、遥测、健康检查
│   ├── ChuckAI.Core/               # 共享 DTO 和模型
│   ├── ChuckAI.Database.Api/       # MCP 服务器 (Azure Functions)
│   │   ├── Functions/
│   │   │   └── JokesFunction.cs    # MCP 工具实现
│   │   └── Program.cs              # MCP 工具注册
│   ├── ChuckAI.Agents/             # AI 代理服务 (Azure Functions)
│   │   ├── Functions/
│   │   │   └── ChatFunction.cs     # 聊天 API 端点
│   │   ├── Services/
│   │   │   ├── ChuckNorrisAgentService.cs  # 带 MCP 工具的 AI 代理
│   │   │   └── McpToolService.cs           # MCP 客户端连接器
│   │   └── Program.cs              # MCP 客户端初始化
│   └── ChuckAI.Web/                # Blazor Web App
└── README.md
```

## 主要依赖

- **Aspire.Hosting** - .NET Aspire 托管和编排
- **Aspire.ServiceDefaults** - 服务发现和遥测的共享配置
- **Microsoft.Azure.Functions.Worker.Extensions.Mcp** - Azure Functions 的 MCP 服务器支持
- **ModelContextProtocol** - MCP 客户端 SDK
- **Microsoft.Agents.AI** - AI 代理框架
- **Microsoft.Agents.AI.OpenAI** - 代理的 OpenAI 集成

## 故障排除

### MCP 连接问题

**问题：** `ECONNREFUSED ::1:7071`

**解决方案：** 在 `McpServerUrl` 配置中使用 `127.0.0.1` 而不是 `localhost`。Windows 可能为 `localhost` 优先使用 IPv6 (`::1`)，但 Azure Functions 默认监听 IPv4。

### 代理不调用工具

**问题：** 代理在不调用 MCP 工具的情况下响应

**解决方案：**
- 验证 MCP 服务器是否在端口 7071 上运行
- 检查代理日志中是否有 MCP 连接错误
- 确保工具在启动时已加载（在日志中查找 "Loaded N tools from MCP server"）

### 数据库连接问题

**问题：** 无法连接到 Azure SQL Database

**解决方案：**
- 验证你的 IP 地址是否在 Azure SQL 防火墙规则的白名单中
- 使用 SQL Server Management Studio 测试连接字符串
- 确保 `local.settings.json` 中的凭据正确

## 了解更多

- [.NET Aspire 文档](https://learn.microsoft.com/dotnet/aspire/)
- [Model Context Protocol 文档](https://modelcontextprotocol.io/)
- [Azure Functions 的 MCP 扩展](https://learn.microsoft.com/azure/azure-functions/)
- [Microsoft Agents AI 文档](https://github.com/microsoft/agents)
- [Azure AI Foundry](https://azure.microsoft.com/products/ai-foundry/)

## 许可证

这是一个用于教育目的的演示项目。

## 作者

在 WPC 2025 展示 - "无服务器 MCP：使用 Azure Function MCP 扩展的远程服务器"
