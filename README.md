# AI Track

A chat application for .NET and AI developers with automated news aggregation and hallucination-free responses.

## Project Overview

This project provides two main features:

1. **AI Chat Without Hallucinations** - Uses a custom .NET AI MCP server (https://github.com/Ahod26/dotnet-ai-mcp-server) that provides access to real-time .NET AI repository code and Microsoft documentation. The AI can fetch actual code examples from repositories like Semantic Kernel, OpenAI .NET SDK, and MCP C# SDK instead of relying on outdated training data. This dramatically reduces hallucinations about .NET AI frameworks.

2. **News Integration** - Automatically collects and aggregates news from specific sources I follow using GitHub MCP and RSS feeds, then allows chatting about these articles with full context. This eliminates hallucinations about recent developments by providing the AI with the actual article content.

## Technologies Used

### Backend

- ASP.NET Core 9.0 (Minimal APIs)
- Entity Framework Core 9.0 with MySQL
- SignalR for WebSocket communication
- OpenAI API (gpt-4.1 + text-embedding-3-small)
- Redis Stack with NRedisStack for vector similarity search
- Custom .NET AI MCP Server (dotnet-ai-mcp-server) via HTTP/SSE
- Model Context Protocol SDK (0.4.0-preview.1)
- GitHub MCP Server (for news aggregation only)
- ASP.NET Identity with JWT authentication
- Google OAuth2
- Serilog for logging
- AutoMapper for DTO mapping

### Frontend

- React 19.1.1
- Vite 7.1.0
- Redux Toolkit 2.8.2 (client state)
- TanStack Query 5.84.2 (server state)
- @microsoft/signalr 9.0.6
- React Router DOM 7.8.0
- react-markdown with syntax highlighting
- CSS Modules

### Infrastructure

- Docker (Redis Stack, GitHub MCP server for news)
- MySQL 8.0
- n8n for workflow automation
- Custom .NET AI MCP Server (HTTP/SSE endpoint)

## News Sources

### GitHub Repositories (via MCP)

- [microsoft/semantic-kernel](https://github.com/microsoft/semantic-kernel)
- [microsoft/kernel-memory](https://github.com/microsoft/kernel-memory)
- [microsoft/autogen](https://github.com/microsoft/autogen)
- [openai/openai-dotnet](https://github.com/openai/openai-dotnet)
- [dotnet/extensions](https://github.com/dotnet/extensions)
- [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)

### RSS Feeds

- [Microsoft .NET DevBlog](https://devblogs.microsoft.com/dotnet/feed/)
- [Semantic Kernel Blog](https://devblogs.microsoft.com/semantic-kernel/feed/)
- [Azure AI/ML Blog](https://azure.microsoft.com/en-us/blog/category/ai-machine-learning/feed/)
- [GitHub AI/ML Blog](https://github.blog/ai-and-ml/feed/)

### YouTube Channels

- Microsoft Developer (@MicrosoftDeveloper)
- .NET (@dotnet)
- OpenAI (@OpenAI)
- Anthropic (@anthropic-ai)
- Microsoft Azure (@MicrosoftAzure)

## Key Features

**Chat System:**

- Real-time streaming with SignalR
- Custom .NET AI MCP server integration for accurate .NET AI information
- Access to 14+ .NET AI repositories (Semantic Kernel, OpenAI SDK, MCP SDK, etc.)
- Microsoft Learn documentation integration as fallback
- Semantic caching with vector similarity (0.85 threshold)
- Dual-layer cache (exact match + semantic)
- Context window management
- Stop generation capability
- 10 chat limit per user

**News System:**

- Daily automated collection (every 24 hours)
- AI-powered filtering for relevance
- Timeline view with search and date filtering
- Chat-from-news integration (full article context)
- Newsletter distribution via n8n

**Other:**

- JWT and Google OAuth2 authentication
- Rate limiting on all endpoints
- Structured logging with Serilog
- Global exception handling

## Setup

### Prerequisites

- .NET 9.0 SDK - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- Node.js 18+ - [Download](https://nodejs.org/)
- MySQL 8.0+ - [Download](https://dev.mysql.com/downloads/mysql/)
- Docker Desktop - [Download](https://www.docker.com/products/docker-desktop)
- API Keys:
  - [OpenAI API Key](https://platform.openai.com/api-keys)
  - [GitHub Personal Access Token](https://github.com/settings/tokens)
  - [Tavily API Key](https://tavily.com/)
  - [Google OAuth2 Credentials](https://console.cloud.google.com/)
  - [YouTube Data API v3 Key](https://console.cloud.google.com/)

### Installation

**1. Clone repository:**

```bash
git clone <repository-url>
cd "Ai Track"
```

**2. Start Docker services:**

```bash
# Redis Stack
docker run -d --name redis-stack -p 6379:6379 -p 8001:8001 redis/redis-stack:latest

# GitHub MCP Server (for news aggregation only)
docker run -d --name mcp-github \
  -e GITHUB_TOKEN=your_github_token \
  -p 8080:8080 \
  ghcr.io/github/github-mcp-server
```

**3. Set up MySQL:**

```bash
mysql -u root -p
CREATE DATABASE ai_track_db;
EXIT;
```

**4. Configure and run backend:**

```bash
cd backend

# Configure appsettings.Development.json with your API keys
# See backend/README.md for details

dotnet restore
dotnet ef database update
dotnet run
```

Backend runs at: `https://localhost:7197` or `http://localhost:5170`

**5. Configure and run frontend:**

```bash
cd frontend

# Update src/api/config.js with backend URL if needed
npm install
npm run dev
```

Frontend runs at: `http://localhost:5173`

## Documentation

- [Backend README](./backend/README.md) - API endpoints, database schema, configuration
- [Frontend README](./frontend/README.md) - Component architecture, hooks, state management
