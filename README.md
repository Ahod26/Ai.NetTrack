# AI Track

A chat application with automated news aggregation for .NET and AI topics. Built to learn about LLM integration, semantic caching, and real-time web applications.

## Project Overview

I built this to address a specific need: getting accurate, up-to-date information about .NET and AI developments without LLM hallucinations. The application aggregates news from specific GitHub repositories, RSS feeds, and YouTube channels I follow, then allows me to chat about these updates with full article context.

### Why I Built This

- Learn how to integrate OpenAI API with streaming responses
- Implement semantic caching using vector embeddings to reduce API costs
- Work with Model Context Protocol (MCP) for external tool integration
- Build a real-time application using SignalR
- Practice full-stack development with .NET 9 and React 19

### What It Does

1. Collects news daily from 6 GitHub repos, RSS feeds, and YouTube channels
2. Filters content using AI to keep only .NET/AI-related items
3. Provides a timeline view with search and filtering
4. Allows chatting with AI about specific news articles (full context included)
5. Caches similar queries using vector similarity to reduce costs
6. Sends daily newsletter to subscribers via n8n automation

## Technologies Used

### Backend
- ASP.NET Core 9.0 (Minimal APIs)
- Entity Framework Core 9.0 with MySQL
- SignalR for WebSocket communication
- OpenAI API (GPT-4o-mini + text-embedding-3-small)
- Redis Stack with NRedisStack for vector similarity search
- Model Context Protocol SDK (0.4.0-preview.1)
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
- Docker (Redis Stack, MCP GitHub server)
- MySQL 8.0
- n8n for workflow automation
- Tavily MCP server (Node.js)

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

### YouTube
- AI tutorial channels
- .NET conference talks

News collection runs daily (every 24 hours) with AI filtering for relevance.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        React Frontend                            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────────────┐│
│  │   Chat   │  │ Timeline │  │ Starred  │  │ Account Settings││
│  │   Page   │  │   Page   │  │ Messages │  │      Page       ││
│  └──────────┘  └──────────┘  └──────────┘  └─────────────────┘│
│         │              │              │              │          │
│         └──────────────┴──────────────┴──────────────┘          │
│                           │                                      │
│                  ┌────────▼────────┐                            │
│                  │  SignalR Client │                            │
│                  │  Redux + Query  │                            │
│                  └────────┬────────┘                            │
└───────────────────────────┼─────────────────────────────────────┘
                            │ HTTP/WebSocket
┌───────────────────────────▼─────────────────────────────────────┐
│                    ASP.NET Core Backend                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │               Controllers & SignalR Hub                   │  │
│  │   Auth │ Chat │ Messages │ News │ Profile │ ChatHub      │  │
│  └────┬────────────────────────────────────────────┬─────────┘  │
│       │                                             │            │
│  ┌────▼──────────────────────────────────────────┐ │            │
│  │                  Services Layer                │ │            │
│  │  Auth │ Chat │ OpenAI │ LLMCache │ News       │ │            │
│  └────┬──────────────────────────────────────────┘ │            │
│       │                                             │            │
│  ┌────▼──────────────────────────────────────────┐ │            │
│  │              Repository Layer                  │ │            │
│  │  User │ Chat │ Message │ News │ Redis         │ │            │
│  └────┬──────────────────────────────────────────┘ │            │
└───────┼─────────────────────────────────────────────┼────────────┘
        │                                             │
        ▼                                             ▼
  ┌──────────┐  ┌──────────────┐            ┌──────────────┐
  │  MySQL   │  │ Redis Stack  │            │   SignalR    │
  │ Database │  │ (Semantic    │            │ (WebSocket)  │
  │          │  │   Cache)     │            │              │
  └──────────┘  └──────────────┘            └──────────────┘
        │
        │
┌───────▼─────────────────────────────────────────────────────────┐
│                   Background Services                            │
│  ┌──────────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ GitHub News  │  │ RSS News │  │ YouTube  │  │   n8n    │   │
│  │  (via MCP)   │  │  Parser  │  │  Fetcher │  │Newsletter│   │
│  └──────┬───────┘  └─────┬────┘  └─────┬────┘  └────┬─────┘   │
│         │                │              │            │          │
│         └────────────────┴──────────────┴────────────┘          │
│                           │                                      │
│                   ┌───────▼────────┐                            │
│                   │  News Database │                            │
│                   └────────────────┘                            │
└──────────────────────────────────────────────────────────────────┘

External Integrations:
  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐
  │  OpenAI API │  │ MCP Servers │  │ Google OAuth │
  │ (GPT-4o-mini│  │ (GitHub +   │  │    (Login)   │
  │  + Embed)   │  │   Tavily)   │  │              │
  └─────────────┘  └─────────────┘  └──────────────┘
```

### Data Flow: Chat-from-News

```
1. User browses Timeline → Sees latest .NET or AI news
2. User clicks "Chat with AI" on news article
3. Frontend: POST /chat with newsItemId
4. Backend: Retrieves full article content from database
5. Backend: Creates new chat with article context included
6. Backend: AI receives article as context (no hallucination)
7. User asks questions → AI responds with article-grounded answers
8. SignalR streams AI response chunks in real-time
```

### Semantic Caching Flow

```
1. User sends message → Service checks cache
2. Hash last N messages → MD5 exact match check
3. If miss → Generate embedding (OpenAI text-embedding-3-small)
4. Vector similarity search in Redis (cosine > 0.85)
5. If hit → Return cached response (< 5ms)
6. If miss → Call OpenAI API (~2-5s)
7. Store response in both caches with decay TTL
8. Return response to user
```

## Setup

### Prerequisites

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **MySQL 8.0+** - [Download](https://dev.mysql.com/downloads/mysql/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop) (for Redis and MCP servers)
- **API Keys**:
  - [OpenAI API Key](https://platform.openai.com/api-keys)
  - [GitHub Personal Access Token](https://github.com/settings/tokens)
  - [Tavily API Key](https://tavily.com/)
  - [Google OAuth2 Credentials](https://console.cloud.google.com/)

### Installation

**1. Clone the repository:**
```bash
git clone <repository-url>
cd "Ai Track"
```

**2. Start external services (Docker):**
```bash
# Redis Stack for semantic caching
docker run -d --name redis-stack -p 6379:6379 -p 8001:8001 redis/redis-stack:latest

# GitHub MCP Server
docker run -d --name mcp-github \
  -e GITHUB_TOKEN=your_github_token \
  -p 8080:8080 \
  modelcontextprotocol/github-server
```

**3. Set up MySQL database:**
```bash
mysql -u root -p
CREATE DATABASE ai_track_db;
EXIT;
```

**4. Configure backend:**
```bash
cd backend

# Configure appsettings.Development.json with your API keys
# See backend/README.md for full configuration details

dotnet restore
dotnet ef database update  # Apply migrations
dotnet run
```

Backend runs at: `https://localhost:7197` (or `http://localhost:5170`)

**5. Configure and run frontend:**
```bash
cd frontend

# Update src/api/config.js with backend URL if needed
npm install
npm run dev
```

Frontend runs at: `http://localhost:5173`

**6. Access the application:**
- Open browser to `http://localhost:5173`
- Sign up or use Google OAuth
- Start chatting or browse the news timeline!

## Documentation

- [Backend README](./backend/README.md) - API endpoints, database schema, configuration
- [Frontend README](./frontend/README.md) - Component architecture, hooks, state management

## What I Learned

- Implementing OpenAI streaming API with IAsyncEnumerable
- Building semantic cache using vector embeddings (cosine similarity)
- Working with Model Context Protocol for external tools
- Real-time communication with SignalR (chunk accumulation, reconnection strategies)
- Background services in .NET with PeriodicTimer
- Dual-state management (Redux + TanStack Query)
- Rate limiting strategies
- Clean architecture with dependency injection
