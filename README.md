# AI Track

> A production-ready AI-powered chat platform for .NET and AI developers, providing accurate, hallucination-free responses with real-time news integration and semantic caching.

## 🎯 Project Overview

**AI Track** is a sophisticated chat application designed specifically for .NET and AI developers who need accurate, up-to-date information without the typical LLM hallucinations. The platform combines real-time AI chat with intelligent news aggregation, allowing developers to stay informed about the latest .NET and AI developments while having context-aware conversations grounded in factual, curated content.

### The Problem

Traditional AI assistants often provide outdated information or hallucinate facts about rapidly evolving technologies like .NET and AI frameworks. Developers need a solution that:
- Provides **accurate, verifiable information** from trusted sources
- Stays **up-to-date** with the latest releases, updates, and best practices
- Allows **context-aware discussions** about specific news articles and releases
- Reduces costs through **intelligent caching** of similar queries

### The Solution

AI Track addresses these challenges through:

1. **Curated News Aggregation** - Daily automated collection from trusted sources:
   - **GitHub Repositories**: semantic-kernel, kernel-memory, autogen, openai-dotnet, dotnet/extensions, mcp-csharp-sdk
   - **RSS Feeds**: Microsoft .NET DevBlog, Semantic Kernel Blog, AI/ML tech blogs
   - **YouTube**: AI tutorial channels and conference talks

2. **Chat-from-News Integration** - Users can click "Chat with AI" on any news item to discuss it with full article context, eliminating hallucinations about recent developments.

3. **Semantic Caching** - Dual-layer caching (vector similarity + exact match) reduces redundant API calls by 40-60%, cutting costs while maintaining accuracy.

4. **MCP Tool Integration** - Real-time access to GitHub repositories and web search via Model Context Protocol, providing verifiable, current information.

## 🚀 Key Features

- **Real-Time AI Chat** with SignalR streaming and context-aware conversations
- **News Timeline** with infinite scroll, search, and date filtering (GitHub releases, RSS, YouTube)
- **Chat-from-News** integration for discussing specific articles with full context
- **Semantic Caching** with vector similarity search (0.85 threshold) for cost optimization
- **MCP Servers** integration (GitHub + Tavily) for external tool access
- **Google OAuth2** and email/password authentication
- **Newsletter System** via n8n automation for daily digest emails
- **Rate Limiting** and global exception handling for production readiness
- **10 chat limit** per user with intelligent context window management

## 🛠️ Technology Stack

### Backend (.NET 9.0)

| Category | Technology | Purpose |
|----------|-----------|---------|
| **Framework** | ASP.NET Core 9.0 | Web API with minimal APIs |
| **Database** | MySQL 8.0+ | Relational data storage |
| **ORM** | Entity Framework Core 9.0 | Code-first migrations |
| **Caching** | Redis Stack | Semantic cache with vector search |
| **Real-Time** | SignalR | WebSocket-based streaming |
| **AI** | OpenAI API (GPT-4o-mini) | LLM for chat responses |
| **MCP** | ModelContextProtocol.AspNetCore 0.4.0 | GitHub & Tavily integration |
| **Auth** | ASP.NET Identity + JWT | Authentication & authorization |
| **Automation** | n8n Webhooks | Newsletter distribution |
| **Logging** | Serilog | Structured logging to MySQL |

### Frontend (React 19)

| Category | Technology | Purpose |
|----------|-----------|---------|
| **Framework** | React 19.1.1 | Modern UI with concurrent features |
| **Build Tool** | Vite 7.1.0 | Fast bundling with HMR |
| **Routing** | React Router DOM 7.8.0 | SPA navigation |
| **State (Client)** | Redux Toolkit 2.8.2 | Auth & UI state |
| **State (Server)** | TanStack Query 5.84.2 | API caching & sync |
| **Real-Time** | @microsoft/signalr 9.0.6 | SignalR client |
| **Content** | react-markdown, react-syntax-highlighter | Markdown & code rendering |
| **Styling** | CSS Modules | Scoped component styles |

## 💡 Skills Demonstrated

### Software Architecture
- **Clean Architecture** with clear separation of concerns (Controllers → Services → Repositories)
- **Dual-state management strategy** (Redux for client state, TanStack Query for server state)
- **Singleton pattern** for SignalR connection management
- **Repository pattern** with dependency injection
- **MCP tool orchestration** with keyword-based selection

### Advanced .NET Development
- **ASP.NET Core 9.0** minimal APIs with global exception handling
- **Entity Framework Core** with 23+ migrations and complex relationships
- **SignalR** for bidirectional real-time communication
- **Background Services** with PeriodicTimer for scheduled tasks
- **Rate limiting** with fixed window policies
- **JWT authentication** with cookie-based session management
- **Google OAuth2** integration

### AI & Machine Learning Integration
- **OpenAI Streaming API** with IAsyncEnumerable for real-time responses
- **Semantic caching** using vector embeddings (text-embedding-3-small)
- **Cosine similarity search** with 0.85 threshold for cache hits
- **TTL decay algorithm** (21 days base × 0.7^message_count)
- **Model Context Protocol** integration for external tool access
- **AI-powered filtering** for news relevance determination

### Frontend Engineering
- **React 19** with functional components and custom hooks
- **SignalR chunk accumulation** for streaming message display
- **Optimistic updates** with rollback on error
- **Infinite scroll** with IntersectionObserver
- **Debounced search** (500ms) for efficient API usage
- **CSS Modules** with camelCase convention

### Database & Caching
- **MySQL** connection pooling (100 connections)
- **Redis Stack** with vector search capabilities
- **NRedisStack** for semantic similarity queries
- **Composite indexes** for optimized queries
- **Timezone-aware** timestamp handling

### DevOps & Production Readiness
- **Docker** for Redis and MCP servers
- **Rate limiting** across multiple endpoints
- **Global exception handling** with ProblemDetails format
- **Structured logging** with Serilog to MySQL
- **API versioning** and OpenAPI documentation
- **CORS configuration** for frontend integration

## 📰 Monitored Resources

The platform automatically aggregates news from the following trusted sources:

### GitHub Repositories (via MCP)
- **[microsoft/semantic-kernel](https://github.com/microsoft/semantic-kernel)** - AI orchestration framework
- **[microsoft/kernel-memory](https://github.com/microsoft/kernel-memory)** - RAG and memory management
- **[microsoft/autogen](https://github.com/microsoft/autogen)** - Multi-agent conversations
- **[openai/openai-dotnet](https://github.com/openai/openai-dotnet)** - Official OpenAI .NET SDK
- **[dotnet/extensions](https://github.com/dotnet/extensions)** - .NET extensions and libraries
- **[modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)** - MCP C# SDK

### RSS Feeds
- **[Microsoft .NET DevBlog](https://devblogs.microsoft.com/dotnet/feed/)** - Official .NET announcements
- **[Semantic Kernel Blog](https://devblogs.microsoft.com/semantic-kernel/feed/)** - SK updates and tutorials
- **AI/ML Technology Blogs** - Industry news and research updates

### YouTube Channels
- **AI Tutorial Channels** - Hands-on coding tutorials and demos
- **Conference Talks** - .NET Conf, Build, Ignite presentations
- **Tech Update Channels** - Weekly AI and .NET news roundups

News is collected daily at startup and every 24 hours thereafter, with AI-powered filtering to ensure relevance to .NET and AI developers.

## 🏗️ Architecture Overview

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

## 🚀 Quick Start

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

## 📖 Documentation

- **[Backend README](./backend/README.md)** - Comprehensive backend documentation (API, architecture, database)
- **[Frontend README](./frontend/README.md)** - Frontend architecture, components, and hooks

## 🎓 Learning Outcomes

This project demonstrates:

- **Full-stack development** with modern .NET and React
- **Real-time communication** using SignalR WebSockets
- **AI integration** with streaming responses and semantic caching
- **MCP implementation** for external tool orchestration
- **Production-ready patterns** (rate limiting, logging, error handling)
- **News aggregation** with multi-source data collection
- **Database design** with complex relationships and migrations
- **Authentication** with multiple providers (JWT + OAuth2)
- **Background services** for scheduled tasks
- **State management** with dual-strategy approach
- **Deployment-ready** architecture with Docker support

## 📝 License

This project is licensed under the MIT License.

## 🙏 Acknowledgments

Built with amazing open-source technologies:

**Backend:** ASP.NET Core, Entity Framework Core, SignalR, OpenAI SDK, Redis Stack, Serilog, AutoMapper

**Frontend:** React, Vite, Redux Toolkit, TanStack Query, SignalR Client, react-markdown

**Infrastructure:** MySQL, Docker, n8n, Model Context Protocol

---

**Created for .NET and AI developers who value accuracy, timeliness, and verifiable information.**
