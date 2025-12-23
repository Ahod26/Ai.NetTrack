# AI Track - Backend

A robust, AI-powered .NET 9.0 backend application that leverages the **Model Context Protocol (MCP)**, **Semantic Caching**, and real-time communication to deliver a sophisticated chat experience. This system integrates with external services like GitHub, YouTube, RSS feeds, and n8n to provide context-aware AI interactions with intelligent news aggregation and automated newsletter distribution.

## Table of Contents

- [Key Features](#-key-features)
- [Tech Stack](#-tech-stack)
- [Architecture Overview](#-architecture-overview)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
- [Configuration Guide](#-configuration-guide)
- [API Documentation](#-api-documentation)
- [Database Schema](#-database-schema)
- [Features Deep Dive](#-features-deep-dive)
- [Background Services](#-background-services)
- [Security & Rate Limiting](#-security--rate-limiting)
- [Testing](#-testing)
- [Development](#-development)
- [Troubleshooting](#-troubleshooting)

## 🚀 Key Features

### AI Chat System with Real-time Streaming

- **Real-time bidirectional communication** via SignalR for instant message streaming
- **Context-aware conversations** with automatic context window management
- **10 chat limit per user** with intelligent quota enforcement
- **Stop generation capability** for long-running responses
- **AI-powered chat titles** automatically generated based on conversation content

### Semantic Caching & LLM Response Optimization

- **Dual-layer caching strategy**:
  - **Semantic caching**: Vector-based similarity search using OpenAI embeddings (0.85 similarity threshold)
  - **Exact match caching**: Fast retrieval for identical queries
- **Intelligent TTL with decay factor**: Older messages cached longer (21 days base with 0.7 decay)
- **Significant cost reduction**: Avoids redundant API calls for similar queries
- **Redis Stack integration** with NRedisStack for advanced caching features

### Model Context Protocol (MCP) Integration

- **Custom .NET AI MCP Server**: Using dotnet-ai-mcp-server via HTTP/SSE for LLM interactions
- **Connected MCP Servers**:
  - **DotNet AI MCP Server** (HTTP): Provides access to 14+ .NET AI repositories and Microsoft Learn documentation
  - **GitHub Server** (Docker): Repository monitoring for news aggregation only
- **Intelligent agentic workflow**: Start_DotNet_Reasoning triggers automatic navigation through repos → folders → files → code
- **Microsoft Learn fallback**: Searches official documentation when GitHub code examples are insufficient

### News Aggregation System

- **Multi-source intelligence**:
  - **GitHub**: Monitors 6 key repositories (semantic-kernel, kernel-memory, autogen, openai-dotnet, extensions, mcp-csharp-sdk)
  - **RSS Feeds**: Microsoft .NET DevBlog, Semantic Kernel Blog, and AI/tech blogs
  - **YouTube**: AI-related video updates and tutorials
- **AI-powered filtering**: Uses LLM to filter relevant news and generate summaries
- **Daily automated collection**: Background service with 30-minute retry on failure
- **News caching**: 2-hour TTL for improved performance

### n8n Workflow Integration

- **Automated newsletter distribution**: Triggers n8n workflows for email delivery
- **Webhook-based communication**: Sends news and subscriber lists to n8n
- **Subscription management**: User newsletter preferences with email list caching
- **Daily newsletter**: Automatically sends today's and yesterday's news to subscribers

### Authentication & Authorization

- **JWT-based authentication**: Secure stateless token management
- **Google OAuth2 integration**: Seamless social login
- **Account security**:
  - Password validation: Minimum 6 characters with digits and lowercase letters
  - Account lockout: 5 failed attempts = 5-minute lockout
  - HttpOnly, Secure cookies with SameSite protection
- **Token refresh**: Automatic JWT regeneration on profile updates

### User Profile Management

- Update email, full name, and password
- Newsletter subscription preferences
- Complete account deletion with cascade
- JWT token regeneration on updates

### Message Management

- **Star important messages**: Mark and retrieve favorite responses
- **Report problematic messages**: Flag inappropriate content with reason
- **Message metadata**: Token counting for context tracking
- **Timezone-aware timestamps**: Consistent time handling across the application

### Rate Limiting & Security

- **Endpoint-specific rate limits**:
  - General: 100 req/min (queue: 10)
  - Chat: 20 req/min (queue: 5)
  - News: 30 req/min (queue: 10)
  - Messages: 15 req/min (prevent toggle abuse)
  - Auth: 20 req/5min per IP
  - Profile: 10 req/10min per IP
  - Global: 1000 req/hour per IP
- **Request timeouts**: 2-5 second range for different operations
- **Global exception handling**: Consistent error responses with ProblemDetails

## 🛠️ Tech Stack

### Core Framework

- **ASP.NET Core 9.0**: Modern web API framework with minimal APIs
- **C# 12+**: Latest language features with nullable reference types
- **.NET 9.0 SDK**: Latest runtime and SDK

### Database & ORM

- **MySQL**: Production-ready relational database with connection pooling (100 connections)
- **Entity Framework Core 9.0**: Code-first ORM with migrations
- **Pomelo.EntityFrameworkCore.MySql 9.0.0-rc.1**: MySQL provider for EF Core

### Caching & Real-time

- **Redis Stack**: Advanced caching with vector search capabilities
- **StackExchange.Redis 2.8.58**: High-performance Redis client
- **NRedisStack 1.1.0**: Redis Stack extensions for semantic search
- **SignalR**: Real-time bidirectional communication for chat streaming

### AI & External Services

- **OpenAI 2.3.0**: Official OpenAI API client for GPT models
- **ModelContextProtocol.AspNetCore 0.4.0-preview.1**: Official MCP SDK
- **Custom DotNet AI MCP Server**: HTTP/SSE connection to dotnet-ai-mcp-server
- **GitHub API**: Repository monitoring via MCP (news aggregation only)
- **n8n**: Workflow automation platform for newsletters

### Authentication & Security

- **Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.8**: User management
- **Microsoft.AspNetCore.Authentication.JwtBearer 9.0.8**: JWT authentication
- **Microsoft.AspNetCore.Authentication.Google 9.0.0**: Google OAuth2

### Additional Libraries

- **AutoMapper 12.0.1**: Object-to-object mapping for DTOs
- **Serilog 9.0.0**: Structured logging with MySQL sink
- **FluentAssertions 8.8.0**: Fluent test assertions
- **Moq 4.20.72**: Mocking framework for unit tests
- **xUnit**: Testing framework with Testcontainers for integration tests

## 🏗️ Architecture Overview

### Project Structure

```
backend/
├── Controllers/              # 5 API controllers (Auth, Chat, Messages, News, Profile)
├── Services/
│   ├── Classes/             # 12+ service implementations
│   └── Interfaces/          # Service contracts
├── Repository/
│   ├── Classes/             # 8 repository implementations
│   └── Interfaces/          # Repository contracts
├── Models/
│   ├── Domain/              # Entity models (ApiUser, Chat, ChatMessage, NewsItem)
│   ├── Dtos/                # 15+ Data Transfer Objects
│   └── Configuration/       # 9 settings classes
├── Hubs/
│   ├── Classes/             # ChatHub for SignalR
│   └── Interfaces/          # IChatClient interface
├── MCP/
│   ├── Classes/             # McpClientService for tool orchestration
│   └── Interfaces/          # IMcpClientService contract
├── Background/
│   ├── Classes/             # 5 background services (GitHub, RSS, YouTube, NewsCollector, N8N)
│   └── Interfaces/          # Background service contracts
├── Extensions/
│   ├── Services/            # DI configuration (Database, Auth, Services, Infrastructure, RateLimiting)
│   └── Identity/            # ClaimsPrincipal extensions
├── Middleware/              # GlobalExceptionHandler
├── Filters/                 # MaxChatsAttribute for chat quota
├── Mapping/                 # AutoMapper profiles
├── Data/                    # ApplicationDbContext
├── Migrations/              # 23+ EF Core migrations
├── Constants/               # ToolTriggers for MCP tool selection
├── Program.cs               # Application entry point and configuration
└── backend.csproj           # Project file with dependencies
```

### Architecture Layers

**Clean Architecture with Separation of Concerns:**

1. **Presentation Layer** (`Controllers/`, `Hubs/`)

   - API endpoints with RESTful design
   - SignalR hubs for real-time communication
   - Request/response DTOs

2. **Business Logic Layer** (`Services/`)

   - Domain logic and orchestration
   - Cache management (LLM, Chat, News, EmailList)
   - Authentication and authorization logic
   - OpenAI integration with streaming support

3. **Data Access Layer** (`Repository/`)

   - Entity Framework Core repositories
   - Redis repository for caching
   - Database operations and queries

4. **Domain Layer** (`Models/Domain/`)

   - Entity models with navigation properties
   - Business rules and validation

5. **Infrastructure Layer** (`Background/`, `MCP/`)
   - External service integrations
   - Background workers and scheduled tasks
   - MCP tool orchestration

### Data Flow

```
Client (React/SignalR)
    ↓
Controllers/Hubs (Presentation)
    ↓
Services (Business Logic)
    ↓ ← → Cache Layer (Redis)
    ↓
Repository (Data Access)
    ↓
Database (MySQL)

External Services:
    OpenAI API ← → Services
    DotNet AI MCP Server (HTTP/SSE) ← → McpClientService
    GitHub MCP (Docker - news only) ← → Background Services
    n8n Webhooks ← → N8NIntegration
```

## 📋 Prerequisites

Ensure you have the following installed on your local machine:

- **[.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** - Required for building and running the application
- **[MySQL Server](https://dev.mysql.com/downloads/mysql/)** - Version 8.0+ recommended
- **[Docker Desktop](https://www.docker.com/products/docker-desktop)** - Required for Redis Stack and GitHub MCP server (news only)
- **[Redis Stack](https://redis.io/docs/getting-started/install-stack/)** - Can be run via Docker
- **API Keys**:
  - OpenAI API key ([Get one here](https://platform.openai.com/api-keys))
  - GitHub Personal Access Token ([Create here](https://github.com/settings/tokens))
  - Tavily API key ([Sign up here](https://tavily.com/))
  - Google OAuth2 credentials ([Get from Google Cloud Console](https://console.cloud.google.com/))

## 🏁 Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd "Ai Track/backend"
```

### 2. Install Dependencies

```bash
dotnet restore
```

### 3. Set Up External Services

#### Redis Stack (Docker)

```bash
docker run -d --name redis-stack -p 6379:6379 -p 8001:8001 redis/redis-stack:latest
```

Verify Redis is running:

```bash
docker ps | grep redis-stack
```

#### MCP Servers

**GitHub MCP Server (Docker - for news aggregation only):**

```bash
docker run -d --name mcp-github \
  -e GITHUB_PERSONAL_ACCESS_TOKEN=your_github_token \
  -i --rm \
  ghcr.io/github/github-mcp-server
```

**DotNet AI MCP Server (for LLM chat):**

This server is hosted publicly at `https://dotnetaimcp.net` and requires no setup. It provides:

- Access to 14+ .NET AI repositories (Semantic Kernel, OpenAI SDK, MCP SDK, etc.)
- Microsoft Learn documentation search
- Code sample search
- Agentic workflow for finding relevant code

For local development or self-hosting:

```bash
git clone https://github.com/Ahod26/dotnet-ai-mcp-server
cd dotnet-ai-mcp-server/DotNetMCPServer
dotnet user-secrets set "GitHub:Token" "your_github_token"
dotnet run
# Server runs at http://localhost:5000
```

### 4. Database Setup

**Create MySQL Database:**

```bash
mysql -u root -p
CREATE DATABASE ai_track_db;
EXIT;
```

**Apply Migrations:**

```bash
dotnet ef database update
```

### 5. Configuration

Create or update `appsettings.Development.json` (see [Configuration Guide](#-configuration-guide) below for full details):

```json
{
  "ConnectionStrings": {
    "AINetTrack": "Server=localhost;Database=ai_track_db;User=root;Password=yourpassword;"
  },
  "JwtSettings": {
    "Issuer": "AiNetTrackAPI",
    "Audience": "AiNetTrackAPI",
    "Key": "YOUR_SUPER_SECRET_KEY_MUST_BE_AT_LEAST_32_CHARACTERS_LONG",
    "ExpirationInMinutes": 10000
  },
  "OpenAI": {
    "ApiKey": "sk-proj-...",
    "Model": "gpt-4o-mini",
    "MaxToken": 4096,
    "Temperature": 0.7
  },
  "McpSettings": {
    "GithubToken": "ghp_...",
    "TavilyApiKey": "tvly-..."
  },
  "Redis": {
    "Configuration": "localhost:6379"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-client-secret"
    }
  },
  "N8N": {
    "NewsletterWebhookUrl": "https://your-n8n-instance.com/webhook/newsletter",
    "ApiKey": "your-n8n-api-key"
  }
}
```

### 6. Run the Application

```bash
dotnet run
```

The backend API will start at:

- HTTPS: `https://localhost:7197`
- HTTP: `http://localhost:5197`

### 7. Verify Setup

**Check API Health:**

```bash
curl https://localhost:7197/auth/status
```

**Access OpenAPI Documentation:**
Navigate to: `https://localhost:7197/openapi/v1.json`

## ⚙️ Configuration Guide

### Complete appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "ConnectionStrings": {
    "AINetTrack": "Server=localhost;Database=ai_track_db;User=root;Password=yourpassword;Pooling=true;MaxPoolSize=100;"
  },

  "JwtSettings": {
    "Issuer": "AiNetTrackAPI",
    "Audience": "AiNetTrackAPI",
    "Key": "YOUR_SUPER_SECRET_KEY_MUST_BE_AT_LEAST_32_CHARACTERS_LONG_FOR_SECURITY",
    "ExpirationInMinutes": 10000
  },

  "OpenAI": {
    "ApiKey": "sk-proj-your-api-key-here",
    "Model": "gpt-4o-mini",
    "MaxToken": 4096,
    "Temperature": 0.7
  },

  "LLMCache": {
    "MaxCacheableMessageCountSemantic": 8,
    "MaxCacheableMessageCountExactMatch": 2,
    "SemanticSimilarityThreshold": 0.85,
    "CacheLifetimeInDays": 21,
    "CacheDecayFactor": 0.7
  },

  "ChatCache": {
    "CacheDurationHours": 2
  },

  "McpSettings": {
    "GithubToken": "ghp_your_github_personal_access_token",
    "TavilyApiKey": "tvly-your-tavily-api-key",
    "YouTubeApiKey": "your-youtube-api-key"
  },

  "Redis": {
    "Configuration": "localhost:6379"
  },

  "Authentication": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-oauth-client-secret"
    }
  },

  "N8N": {
    "NewsletterWebhookUrl": "https://your-n8n-instance.com/webhook/newsletter",
    "ApiKey": "your-n8n-api-key-or-token"
  }
}
```

### Configuration Sections Explained

#### OpenAI Settings

- **ApiKey**: Your OpenAI API key (required)
- **Model**: GPT model to use (default: `gpt-4o-mini` for cost efficiency)
- **MaxToken**: Maximum tokens per response (4096 recommended)
- **Temperature**: Response creativity (0.0-1.0, default: 0.7)

#### LLM Cache Settings

- **MaxCacheableMessageCountSemantic**: Max messages for semantic caching (8)
- **MaxCacheableMessageCountExactMatch**: Max messages for exact match (2)
- **SemanticSimilarityThreshold**: Cosine similarity threshold (0.85 = 85% similar)
- **CacheLifetimeInDays**: Base cache lifetime (21 days)
- **CacheDecayFactor**: Decay multiplier for older messages (0.7)

#### Chat Cache Settings

- **CacheDurationHours**: How long to cache recent chats (2 hours)

#### MCP Settings

- **GitHub.Token**: GitHub Personal Access Token with repo read access (for news aggregation)
- **DotNetAIMcp.Endpoint**: DotNet AI MCP Server endpoint (default: https://dotnetaimcp.net)
- **YouTubeApiKey**: YouTube Data API v3 key (optional, for YouTube news)

#### JWT Settings

- **Issuer**: Token issuer identifier
- **Audience**: Token audience identifier
- **Key**: Secret key (must be at least 32 characters)
- **ExpirationInMinutes**: Token lifetime (10000 = ~7 days)

#### Google OAuth2

- **ClientId**: OAuth 2.0 Client ID from Google Cloud Console
- **ClientSecret**: OAuth 2.0 Client Secret

#### n8n Integration

- **NewsletterWebhookUrl**: n8n webhook endpoint for newsletter
- **ApiKey**: Authentication token for n8n webhook

### Environment Variables (Alternative)

You can also use environment variables instead of appsettings.json:

```bash
export ConnectionStrings__AINetTrack="Server=localhost;..."
export OpenAI__ApiKey="sk-proj-..."
export JwtSettings__Key="YOUR_SECRET_KEY"
export MCP__GitHub__Token="ghp_..."
export MCP__DotNetAIMcp__Endpoint="https://dotnetaimcp.net"
export Authentication__Google__ClientId="..."
export Authentication__Google__ClientSecret="..."
```

### User Secrets (Development)

For local development, use .NET User Secrets to keep sensitive data out of source control:

```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-..."
dotnet user-secrets set "JwtSettings:Key" "YOUR_SECRET_KEY"
dotnet user-secrets set "MCP:GitHub:Token" "ghp_..."
dotnet user-secrets set "MCP:DotNetAIMcp:Endpoint" "https://dotnetaimcp.net"
```

## 📚 API Documentation

### Base URL

- Development: `https://localhost:7197`
- Production: Configure in deployment

### Authentication

All protected endpoints require JWT token in cookies or Authorization header:

```
Authorization: Bearer <jwt_token>
```

---

### Auth Controller (`/auth`)

#### Register User

```http
POST /auth
Content-Type: application/json

{
  "email": "user@example.com",
  "fullName": "John Doe",
  "password": "password123"
}
```

**Response:** `200 OK`

```json
{
  "message": "User created successfully"
}
```

**Validation:**

- Email must be unique and valid format
- Password: minimum 6 characters, must contain digits and lowercase
- FullName: required

---

#### Login

```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:** `200 OK` + HttpOnly cookie with JWT

```json
{
  "message": "Login successful"
}
```

**Account Lockout:** 5 failed attempts = 5-minute lockout

---

#### Check Authentication Status

```http
GET /auth/status
```

**Response:** `200 OK`

```json
{
  "isAuthenticated": true,
  "email": "user@example.com",
  "fullName": "John Doe"
}
```

---

#### Logout

```http
POST /auth/logout
```

**Response:** `200 OK` + clears auth cookie

```json
{
  "message": "Logout successful"
}
```

---

#### Google OAuth Login

```http
GET /auth/google-login
```

**Response:** Redirects to Google OAuth consent screen

---

#### Google OAuth Callback

```http
GET /auth/google-response
```

**Response:** Redirects to frontend with auth token in cookie

---

### Chat Controller (`/chat`)

**Rate Limit:** 20 requests/minute per user

#### Create Chat with First Message

```http
POST /chat
Authorization: Bearer <token>
Content-Type: application/json

{
  "message": "Tell me about semantic caching",
  "newsItemId": null  // Optional: link chat to news item
}
```

**Response:** `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Semantic Caching Discussion",
  "createdAt": "2025-12-23T10:30:00Z",
  "lastMessageAt": "2025-12-23T10:30:00Z",
  "messageCount": 2
}
```

**Constraints:**

- Maximum 10 chats per user (enforced via `MaxChatsAttribute`)
- First message creates the chat
- AI automatically generates chat title

---

#### Get User's Chats

```http
GET /chat
Authorization: Bearer <token>
```

**Response:** `200 OK`

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Semantic Caching Discussion",
    "createdAt": "2025-12-23T10:30:00Z",
    "lastMessageAt": "2025-12-23T10:35:00Z",
    "messageCount": 8,
    "isContextFull": false
  }
]
```

**Cached:** 2 hours

---

#### Delete Chat

```http
DELETE /chat/{chatId}
Authorization: Bearer <token>
```

**Response:** `200 OK`

```json
{
  "message": "Chat deleted successfully"
}
```

**Cascade:** Deletes all messages in the chat

---

#### Update Chat Title

```http
PATCH /chat/{chatId}/title
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "New Chat Title"
}
```

**Response:** `200 OK`

**Validation:** Title must be 1-20 characters

---

### Messages Controller (`/messages`)

**Rate Limit:** 15 requests/minute per user

#### Get Starred Messages

```http
GET /messages/starred
Authorization: Bearer <token>
```

**Response:** `200 OK`

```json
[
  {
    "id": "1fa85f64-5717-4562-b3fc-2c963f66afa6",
    "chatId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "content": "Here's an excellent explanation...",
    "type": "Assistant",
    "createdAt": "2025-12-23T10:32:00Z",
    "tokenCount": 256,
    "isStarred": true,
    "isReported": false
  }
]
```

---

#### Toggle Star Status

```http
PATCH /messages/{messageId}/starred
Authorization: Bearer <token>
```

**Response:** `200 OK`

```json
{
  "isStarred": true
}
```

---

#### Report Message

```http
PATCH /messages/{messageId}/report
Authorization: Bearer <token>
Content-Type: application/json

{
  "reason": "Inappropriate content or inaccurate information"
}
```

**Response:** `200 OK`

```json
{
  "isReported": true,
  "reportReason": "Inappropriate content or inaccurate information",
  "reportedAt": "2025-12-23T10:35:00Z"
}
```

---

### News Controller (`/news`)

**Rate Limit:** 30 requests/minute per user

#### Get News by Date and Type

```http
GET /news?dateTime=2025-12-23T00:00:00Z&newsType=1
Authorization: Bearer <token>
```

**Query Parameters:**

- `dateTime` (optional): Filter by date
- `newsType` (optional): 1=GitHub, 2=RSS, 3=YouTube
- `startDate` (optional): Date range start
- `endDate` (optional): Date range end

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "title": "Semantic Kernel 2.0 Released",
    "content": "Full content of the news article...",
    "url": "https://github.com/microsoft/semantic-kernel/releases/tag/v2.0.0",
    "imageUrl": "https://avatars.githubusercontent.com/...",
    "sourceType": 1,
    "sourceName": "semantic-kernel",
    "publishedDate": "2025-12-22T15:00:00Z",
    "summary": "AI-generated summary of the release notes..."
  }
]
```

**Cached:** 2 hours

---

#### Search News

```http
GET /news/search?searchTerm=semantic+kernel
Authorization: Bearer <token>
```

**Response:** `200 OK` (same structure as Get News)

---

### Profile Controller (`/profile`)

**Rate Limit:** 10 requests/10 minutes per IP

#### Update Email

```http
PUT /profile/email
Authorization: Bearer <token>
Content-Type: application/json

{
  "email": "newemail@example.com"
}
```

**Response:** `200 OK` + new JWT token

```json
{
  "message": "Email updated successfully"
}
```

---

#### Update Full Name

```http
PUT /profile/username
Authorization: Bearer <token>
Content-Type: application/json

{
  "fullName": "Jane Doe"
}
```

**Response:** `200 OK` + new JWT token

---

#### Change Password

```http
PUT /profile/password
Authorization: Bearer <token>
Content-Type: application/json

{
  "currentPassword": "oldpassword123",
  "newPassword": "newpassword456"
}
```

**Response:** `200 OK`

**Validation:** New password must meet password policy

---

#### Toggle Newsletter Subscription

```http
PUT /profile/newsletter
Authorization: Bearer <token>
Content-Type: application/json

{
  "isSubscribed": true
}
```

**Response:** `200 OK`

```json
{
  "isSubscribedToNewsletter": true
}
```

---

#### Delete Account

```http
DELETE /profile
Authorization: Bearer <token>
```

**Response:** `200 OK`

```json
{
  "message": "User deleted successfully"
}
```

**Cascade:** Deletes all user data (chats, messages, profile)

---

### SignalR Hub (`/chathub`)

**Connection:** WebSocket or Server-Sent Events

#### Hub Methods (Client → Server)

**JoinChat**

```javascript
connection.invoke("JoinChat", chatId);
```

**SendMessage**

```javascript
connection.invoke("SendMessage", chatId, messageContent);
```

**StopGeneration**

```javascript
connection.invoke("StopGeneration", chatId);
```

#### Client Methods (Server → Client)

**ReceiveMessage**

```javascript
connection.on("ReceiveMessage", (message) => {
  // message: { id, chatId, content, type, createdAt, tokenCount }
});
```

**ChatJoined**

```javascript
connection.on("ChatJoined", (chatId, messages) => {
  // Confirmation of chat connection + message history
});
```

**Error**

```javascript
connection.on("Error", (errorMessage) => {
  // Error notification
});
```

#### Streaming Flow

1. Client sends message via `SendMessage`
2. Server processes with OpenAI streaming
3. Server sends chunks via `ReceiveMessage` as they arrive
4. Client can stop generation with `StopGeneration`
5. Final message includes complete content

---

### Rate Limiting Policies

| Endpoint | Limit    | Window | Queue Limit |
| -------- | -------- | ------ | ----------- |
| General  | 100 req  | 1 min  | 10          |
| Chat     | 20 req   | 1 min  | 5           |
| News     | 30 req   | 1 min  | 10          |
| Messages | 15 req   | 1 min  | 0           |
| Auth     | 20 req   | 5 min  | 0           |
| Profile  | 10 req   | 10 min | 0           |
| Global   | 1000 req | 1 hour | -           |

**Rate Limit Headers:**

```
X-RateLimit-Limit: 20
X-RateLimit-Remaining: 15
X-RateLimit-Reset: 1640000000
```

**429 Response:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.29",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Please try again later."
}
```

## 🗄️ Database Schema

### Entity Models

#### ApiUser (extends IdentityUser)

```csharp
public class ApiUser : IdentityUser
{
    public string FullName { get; set; }
    public bool IsSubscribedToNewsletter { get; set; }

    // Navigation Properties
    public virtual ICollection<Chat> Chats { get; set; }
}
```

**Inherits from IdentityUser:**

- Id (string - PK)
- Email
- UserName
- PasswordHash
- SecurityStamp
- EmailConfirmed
- PhoneNumber
- LockoutEnd (for account lockout)
- AccessFailedCount
- And other ASP.NET Identity properties

---

#### Chat

```csharp
public class Chat
{
    public Guid Id { get; set; }                    // Primary Key
    public string UserId { get; set; }               // Foreign Key to ApiUser
    public string Title { get; set; }                // AI-generated title
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public bool IsContextFull { get; set; }          // Context window tracking
    public bool isChatRelatedToNewsSource { get; set; }
    public string? relatedNewsSourceURL { get; set; }
    public string? relatedNewsSourceContent { get; set; }  // LONGTEXT

    // Navigation Properties
    public virtual ApiUser User { get; set; }
    public virtual ICollection<ChatMessage> Messages { get; set; }
}
```

**Indexes:**

- `IX_Chats_UserId` (non-clustered)
- `IX_Chats_UserId_LastMessageAt` (composite, non-clustered)

**Constraints:**

- Maximum 10 chats per user (enforced in application layer)
- Cascade delete: Deleting user deletes all their chats

---

#### ChatMessage

```csharp
public class ChatMessage
{
    public Guid Id { get; set; }                     // Primary Key
    public Guid ChatId { get; set; }                  // Foreign Key to Chat
    public string Content { get; set; }               // Message content
    public MessageType Type { get; set; }             // User or Assistant
    public DateTime CreatedAt { get; set; }
    public int TokenCount { get; set; }               // For context tracking
    public bool IsStarred { get; set; }
    public bool IsReported { get; set; }
    public string? ReportReason { get; set; }
    public DateTime? ReportedAt { get; set; }

    // Navigation Property
    public virtual Chat Chat { get; set; }
}

public enum MessageType
{
    User = 0,
    Assistant = 1
}
```

**Indexes:**

- `IX_ChatMessages_ChatId` (non-clustered)
- `IX_ChatMessages_ChatId_CreatedAt` (composite, non-clustered)

**Constraints:**

- Cascade delete: Deleting chat deletes all its messages

---

#### NewsItem

```csharp
public class NewsItem
{
    public int Id { get; set; }                      // Primary Key (auto-increment)
    public string Title { get; set; }
    public string Content { get; set; }
    public string Url { get; set; }
    public string ImageUrl { get; set; }
    public NewsSourceType SourceType { get; set; }
    public string SourceName { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? Summary { get; set; }              // AI-generated summary
}

public enum NewsSourceType
{
    Github = 1,
    Rss = 2,
    Youtube = 3
}
```

**Indexes:**

- `IX_NewsItems_SourceType` (non-clustered)
- `IX_NewsItems_PublishedDate` (non-clustered)
- `IX_NewsItems_SourceType_PublishedDate` (composite, non-clustered)
- `IX_NewsItems_Url` (unique, non-clustered)

---

### Database Relationships

```
ApiUser (1) ──────< (N) Chat
    ↓
IdentityUser (ASP.NET Identity)

Chat (1) ──────< (N) ChatMessage

NewsItem (independent, no FK relationships)
```

### Migration Commands

**Create new migration:**

```bash
dotnet ef migrations add MigrationName
```

**Update database:**

```bash
dotnet ef database update
```

**Rollback to specific migration:**

```bash
dotnet ef database update MigrationName
```

**Remove last migration:**

```bash
dotnet ef migrations remove
```

**Generate SQL script:**

```bash
dotnet ef migrations script
```

**List migrations:**

```bash
dotnet ef migrations list
```

## 🔍 Features Deep Dive

### AI Chat System

The chat system leverages SignalR for real-time bidirectional communication, enabling seamless streaming of AI responses.

**Architecture:**

```
Client (React)
    ↓ WebSocket
ChatHub (SignalR)
    ↓
ChatService
    ↓ ← → LLMCacheService (Semantic + Exact Cache)
    ↓
OpenAIService (Streaming)
    ↓
OpenAI API (GPT-4o-mini)
```

**Key Features:**

1. **Streaming Responses:**

   - Uses `IAsyncEnumerable<string>` for chunked streaming
   - Real-time token-by-token delivery via SignalR
   - Cancellation support with `CancellationToken`

2. **Context Management:**

   - Tracks token count per message
   - Marks chat as `IsContextFull` when nearing limit
   - Automatically includes relevant history in requests

3. **Chat Limits:**

   - Maximum 10 chats per user (enforced via `MaxChatsAttribute`)
   - Prevents resource exhaustion
   - Clear error messages when limit reached

4. **AI-Generated Titles:**
   - First message triggers title generation
   - Uses separate OpenAI call with prompt: "Generate a short, descriptive title (max 20 chars) for this chat"
   - Updates chat entity asynchronously

**Code Example (ChatHub.cs:70-150):**

```csharp
public async Task SendMessage(Guid chatId, string content)
{
    var userId = Context.User!.GetUserId();
    var cancellationToken = GetOrCreateToken(chatId);

    try
    {
        await foreach (var chunk in _chatService.SendMessageAsync(
            userId, chatId, content, cancellationToken))
        {
            await Clients.Group(chatId.ToString())
                .ReceiveMessage(chunk);
        }
    }
    catch (OperationCanceledException)
    {
        // Generation stopped by user
    }
}
```

---

### Semantic Caching

The semantic caching system dramatically reduces API costs and latency by caching LLM responses based on semantic similarity rather than exact matches.

**How It Works:**

1. **Message Grouping:**

   - Groups last N messages into a single query string
   - Configurable: 8 messages for semantic, 2 for exact match

2. **Exact Match Check (Fast Path):**

   - MD5 hash of query for O(1) lookup
   - Returns cached response immediately if found
   - Shortest TTL (for frequent, identical queries)

3. **Semantic Similarity Search (Slow Path):**

   - Generates embedding using OpenAI `text-embedding-3-small`
   - Stores vector in Redis with NRedisStack
   - Performs cosine similarity search
   - Returns cached response if similarity > 0.85 threshold

4. **Cache Miss:**
   - Proceeds with OpenAI API call
   - Stores response in both exact and semantic caches
   - Calculates TTL with decay factor

**TTL Calculation:**

```csharp
TimeSpan ttl = TimeSpan.FromDays(
    baseDays * Math.Pow(decayFactor, messageCount - minMessages)
);
// Example: 21 days * 0.7^(8-2) = ~5 days for 8 messages
```

**Architecture:**

```
Query
  ↓
LLMCacheService
  ↓
[Exact Match Check] → Redis (MD5 hash)
  ↓ (miss)
[Semantic Search] → Redis (Vector similarity)
  ↓ (miss)
OpenAI API Call
  ↓
[Store in Cache] → Redis (both exact + semantic)
  ↓
Response
```

**Redis Keys:**

```
llm_cache:{md5_hash}                    # Exact match
llm_cache:semantic:{embedding_id}       # Semantic vector
llm_cache:embedding:{embedding_id}      # Vector data
```

**Benefits:**

- **Cost Reduction**: 40-60% reduction in OpenAI API calls (estimated)
- **Latency Improvement**: <5ms for cache hits vs ~2-5s for API calls
- **Semantic Understanding**: Matches similar questions with different wording

**Code Reference:** `Services/Classes/LLMCacheService.cs:50-180`

---

### Model Context Protocol (MCP) Integration

MCP extends the LLM's capabilities by connecting it to external tools and data sources, enabling the AI to access real-time code and documentation.

**Connected MCP Servers:**

1. **DotNet AI MCP Server** (HTTP/SSE: `https://dotnetaimcp.net`)

   - **Purpose**: Provides accurate, up-to-date .NET AI information for chat interactions
   - **Transport**: HTTP with Streamable HTTP protocol
   - **Tools**:
     - `Start_DotNet_Reasoning`: REQUIRED entry point for .NET AI questions (agentic workflow)
     - `github_get_folders`: Navigate repository structure
     - `github_list_files`: List files in specific folders
     - `github_fetch_files`: Fetch actual code examples
     - `microsoft_docs_search`: Search Microsoft Learn documentation (fallback)
     - `microsoft_docs_fetch`: Fetch complete documentation pages
     - `microsoft_code_sample_search`: Find official code samples
   - **Tracked Repositories** (14+):
     - AI Frameworks: semantic-kernel, autogen, kernel-memory, extensions, csharp-sdk, langchain
     - LLM SDKs: openai-dotnet, dotnet-genai, anthropic-sdk-csharp
     - Vector DBs: pinecone-dotnet-client, qdrant-dotnet, weaviate-dotnet-client, nredisstack
     - Local LLM: ollamasharp

2. **GitHub MCP Server** (Docker: `ghcr.io/github/github-mcp-server`)
   - **Purpose**: Repository monitoring for news aggregation ONLY (not used in chat)
   - **Tools**:
     - `github_list_releases`: Get repository releases for news
     - `github_get_file_contents`: Read file contents for news context

**Agentic Workflow for Chat:**

The DotNet AI MCP server uses an intelligent agentic workflow that automatically:

1. Identifies available .NET AI repositories
2. Navigates to relevant folders (e.g., `/samples/`, `/docs/`)
3. Lists files to find specific examples
4. Fetches actual code files (.cs) and documentation (.md)
5. Falls back to Microsoft Learn docs if needed

Example flow:

```
User: "How do I create an MCP server in C#?"
  ↓
LLM calls: Start_DotNet_Reasoning
  ↓ (returns available repos)
LLM calls: github_get_folders → modelcontextprotocol::csharp-sdk
  ↓ (finds /samples/ folder)
LLM calls: github_list_files → /samples/QuickstartWeatherServer/
  ↓ (finds Program.cs)
LLM calls: github_fetch_files → actual working code
  ↓
LLM generates response with real code examples
```

**Architecture:**

```
OpenAIService
    ↓
[User asks .NET AI question]
    ↓
GetEssentialTools() → DotNet AI MCP tools only
    ↓
OpenAI (with tool definitions)
    ↓ (tool call: Start_DotNet_Reasoning)
McpClientService → HTTP POST to dotnetaimcp.net
    ↓
[Agentic workflow executes]
    ↓ (returns repos → folders → files → code)
OpenAI synthesizes final response
    ↓
User receives accurate answer with real code
```

**Benefits:**

- **No hallucinations**: Real code from actual repositories
- **Always current**: Fetches latest code, not training data
- **Token efficient**: Gradual exposure (repos → folders → files)
- **Automatic workflow**: LLM orchestrates tools intelligently

**Configuration:**

```json
{
  "MCP": {
    "GitHub": {
      "Token": "ghp_..." // For news aggregation only
    },
    "DotNetAIMcp": {
      "Endpoint": "https://dotnetaimcp.net" // For chat interactions
    }
  }
}
```

**Code Reference:** `MCP/Classes/McpClientService.cs:80-115`

---

### News Aggregation System

The news aggregation system automatically collects, filters, and summarizes AI/tech news from multiple sources.

**Background Service Flow:**

```
NewsAggregationService (starts on app launch)
    ↓ (daily timer)
NewsCollectorService.CollectNewsFromAllSourcesAsync()
    ↓ (parallel execution)
    ├─→ GitHubService.GetLatestNewsAsync()
    ├─→ RssService.GetLatestNewsAsync()
    └─→ YouTubeService.GetLatestNewsAsync()
    ↓ (aggregate results)
[AI Filtering & Summarization]
    ↓ (save to DB)
NewsItemsRepo.SaveNewsItemsAsync()
    ↓ (if news found)
N8NIntegration.TriggerNewsletterWorkflowAsync()
```

**News Sources:**

1. **GitHub (6 repositories):**

   - Monitors releases via `github_list_releases` MCP tool
   - Gets last 5 releases per repo
   - AI filters for relevance (e.g., excludes minor bug fixes)
   - Extracts: title, description, URL, release date

2. **RSS Feeds:**

   - Microsoft .NET DevBlog
   - Semantic Kernel Blog
   - AI/ML tech blogs
   - Parses XML/Atom feeds
   - AI summarizes long articles

3. **YouTube:**
   - AI tutorial channels
   - Conference talks
   - Tech updates
   - Extracts: title, description, video URL, thumbnail

**AI-Powered Filtering:**

Each source uses OpenAI to determine relevance:

```csharp
var prompt = $@"
Analyze this content and determine if it's relevant to AI/ML developers:
Title: {title}
Content: {content}

Return JSON: {{ ""isRelevant"": true/false, ""summary"": ""..."" }}
";
```

**Scheduling:**

- **Primary**: Runs daily at startup + 24-hour intervals
- **Retry**: If failure occurs, retries after 30 minutes
- **Initialization**: 3-second delay on startup to allow MCP servers to initialize

**Caching:**

- News cached for 2 hours in Redis
- Cache key: `news:{sourceType}:{date}`
- Reduces database load for frequent requests

**Code Reference:** `Background/Classes/NewsAggregationService.cs:30-120`

---

### n8n Workflow Integration

The n8n integration automates newsletter distribution to subscribed users.

**Workflow Trigger:**

```
NewsAggregationService (after collecting news)
    ↓ (if news found)
N8NIntegration.TriggerNewsletterWorkflowAsync()
    ↓ (HTTP POST)
n8n Webhook Endpoint
    ↓ (n8n workflow)
    ├─→ Format newsletter HTML
    ├─→ Personalize for each subscriber
    └─→ Send emails via SMTP/SendGrid
```

**Payload Sent to n8n:**

```json
{
  "timestamp": "2025-12-23T10:00:00Z",
  "todayNews": [
    {
      "title": "Semantic Kernel 2.0 Released",
      "summary": "Major update with...",
      "url": "https://github.com/...",
      "sourceType": 1,
      "publishedDate": "2025-12-22T15:00:00Z"
    }
  ],
  "yesterdayNews": [...],
  "subscribers": [
    "user1@example.com",
    "user2@example.com"
  ]
}
```

**n8n Workflow (Example):**

1. **Webhook Trigger**: Receives payload from backend
2. **Function Node**: Formats news items into HTML template
3. **Loop Over Subscribers**: Personalize email for each user
4. **Send Email**: Uses Gmail/SendGrid node
5. **Success Response**: Returns 200 OK

**Email List Caching:**

- Subscriber emails cached for 2 hours
- Cache invalidated on user subscription changes
- Reduces database queries

**Authentication:**

- API key sent in `Authorization` header
- n8n validates key before processing

**Code Reference:** `Background/Classes/N8NIntegration.cs:25-90`

## 🔄 Background Services

### NewsAggregationService

**Lifecycle:**

```csharp
public class NewsAggregationService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); // Initial delay

        using PeriodicTimer timer = new(TimeSpan.FromDays(1));

        do
        {
            try
            {
                await InitializeMcpClientsAsync();
                var newsCount = await _newsCollector.CollectNewsFromAllSourcesAsync();

                if (newsCount > 0)
                    await _n8nIntegration.TriggerNewsletterWorkflowAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "News aggregation failed");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Retry delay
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
```

**Features:**

- **PeriodicTimer**: Modern .NET timer for scheduled tasks
- **Graceful Shutdown**: Respects `CancellationToken` for clean app shutdown
- **Error Recovery**: Catches exceptions and retries after delay
- **MCP Initialization**: Ensures MCP clients are ready before collection

**Logging:**

```
[10:00:00 INF] Starting news aggregation service
[10:00:03 INF] Initializing MCP clients
[10:00:05 INF] Collecting news from GitHub
[10:00:12 INF] Collecting news from RSS feeds
[10:00:18 INF] Collecting news from YouTube
[10:00:22 INF] Collected 15 news items
[10:00:23 INF] Triggering n8n newsletter workflow
[10:00:25 INF] Newsletter sent to 42 subscribers
```

**Manual Trigger (Development):**

You can manually trigger news collection:

```bash
# Via SignalR or direct service call in Startup
services.GetRequiredService<NewsCollectorService>().CollectNewsFromAllSourcesAsync();
```

---

### News Collection Pipeline

Each collector implements `INewsSourceService`:

```csharp
public interface INewsSourceService
{
    Task<List<NewsItem>> GetLatestNewsAsync();
}
```

**GitHubService:**

```csharp
public async Task<List<NewsItem>> GetLatestNewsAsync()
{
    var allNews = new List<NewsItem>();

    foreach (var repo in _monitoredRepos)
    {
        // Call GitHub MCP: github_list_releases
        var releases = await _mcpClient.ExecuteToolAsync(
            "github_list_releases",
            new { repository = repo, count = 5 }
        );

        // Filter with AI
        foreach (var release in releases)
        {
            var isRelevant = await _openAI.DetermineRelevanceAsync(release);
            if (isRelevant)
                allNews.Add(MapToNewsItem(release));
        }
    }

    return allNews;
}
```

**RssService:**

```csharp
public async Task<List<NewsItem>> GetLatestNewsAsync()
{
    var allNews = new List<NewsItem>();

    foreach (var feedUrl in _rssFeeds)
    {
        var feed = await SyndicationFeed.LoadAsync(feedUrl);

        foreach (var item in feed.Items.Take(10))
        {
            var summary = await _openAI.SummarizeAsync(item.Description);
            allNews.Add(new NewsItem
            {
                Title = item.Title,
                Content = item.Description,
                Url = item.Link,
                Summary = summary,
                SourceType = NewsSourceType.Rss,
                PublishedDate = item.PublishDate
            });
        }
    }

    return allNews;
}
```

**Deduplication:**

- Unique index on `NewsItems.Url` prevents duplicates
- Database constraint violation silently ignored for existing items

---

## 🔒 Security & Rate Limiting

### Authentication Flow

**JWT Authentication:**

```
1. User logs in with email/password
   ↓
2. AuthService validates credentials
   ↓
3. TokenService generates JWT with claims:
   - UserId (sub)
   - Email (email)
   - FullName (name)
   - Issuer (iss)
   - Audience (aud)
   - Expiration (exp)
   ↓
4. CookieService sets HttpOnly, Secure, SameSite cookie
   ↓
5. Client includes cookie in subsequent requests
   ↓
6. JwtBearerMiddleware validates token
   ↓
7. User identity populated in HttpContext.User
```

**Google OAuth2 Flow:**

```
1. Client clicks "Sign in with Google"
   ↓
2. Backend redirects to Google OAuth consent screen
   ↓
3. User grants permissions
   ↓
4. Google redirects to /auth/google-response with code
   ↓
5. Backend exchanges code for access token
   ↓
6. Backend retrieves user info from Google
   ↓
7. AuthService creates or updates user in database
   ↓
8. TokenService generates JWT
   ↓
9. Redirect to frontend with auth cookie
```

### Password Security

**Password Policy:**

- Minimum 6 characters
- Must contain at least one digit
- Must contain at least one lowercase letter
- No uppercase or special character requirement (for user convenience)

**Account Lockout:**

- 5 failed login attempts triggers lockout
- Lockout duration: 5 minutes
- Lockout end time stored in `ApiUser.LockoutEnd`

**Implementation:**

```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.AllowedForNewUsers = true;
```

### JWT Token Security

**Token Configuration:**

- Algorithm: HS256 (HMAC-SHA256)
- Secret key: Minimum 32 characters (configured in appsettings)
- Expiration: 10,000 minutes (~7 days)
- Issuer/Audience validation enabled

**Token Validation:**

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSettings.Issuer,
    ValidAudience = jwtSettings.Audience,
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtSettings.Key)
    )
};
```

**Token Refresh:**

- Automatic refresh on profile updates
- Old token invalidated (via cookie replacement)
- New token issued with updated claims

### Cookie Security

**Cookie Configuration:**

```csharp
options.Cookie.HttpOnly = true;       // Prevents XSS access
options.Cookie.Secure = true;         // HTTPS only
options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF protection
options.Cookie.Name = "AITrack.Auth";
options.Cookie.Path = "/";
options.ExpireTimespan = TimeSpan.FromMinutes(10000);
```

### CORS Configuration

**Allowed Origins:**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // Vite dev server
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for cookies
    });
});
```

**Production:** Update `WithOrigins()` to include production frontend URL

### Rate Limiting Configuration

**Implementation:**

```csharp
[EnableRateLimiting("GeneralRateLimit")]
public class ChatController : ControllerBase
{
    // 20 requests per minute for chat endpoints
}
```

**Rate Limiter Policies:**

```csharp
// General endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("GeneralRateLimit", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 10;
    });
});

// Auth endpoints (stricter)
options.AddFixedWindowLimiter("AuthRateLimit", opt =>
{
    opt.Window = TimeSpan.FromMinutes(5);
    opt.PermitLimit = 20;
    opt.QueueLimit = 0;  // No queueing
});
```

**Custom Rate Limit Responses:**

```csharp
options.OnRejected = async (context, token) =>
{
    context.HttpContext.Response.StatusCode = 429;
    await context.HttpContext.Response.WriteAsync(
        "Too many requests. Please try again later.",
        cancellationToken: token
    );
};
```

### Request Timeouts

**Configuration:**

```csharp
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    // Longer timeout for chat streaming
    options.AddPolicy("ChatTimeout", TimeSpan.FromMinutes(5));
});
```

### Global Exception Handling

**Middleware:**

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server Error",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = 500;
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails, cancellationToken
        );

        return true;
    }
}
```

**Benefits:**

- Consistent error responses
- Centralized logging
- Prevents sensitive exception details from leaking
- Returns RFC 7807 Problem Details format

## 🧪 Testing

### Test Projects

1. **backend.UnitTests**: Unit tests for services
2. **backend.IntegrationTests**: End-to-end API tests with Testcontainers

### Unit Tests

**Structure:**

```
backend.UnitTests/
├── Services/
│   ├── AuthServiceTests.cs
│   ├── ChatServiceTests.cs
│   ├── LLMCacheServiceTests.cs
│   ├── NewsServiceTests.cs
│   └── OpenAIServiceTests.cs
```

**Example Test:**

```csharp
public class ChatServiceTests
{
    private readonly Mock<IChatRepository> _mockChatRepo;
    private readonly Mock<IOpenAIService> _mockOpenAI;
    private readonly ChatService _chatService;

    public ChatServiceTests()
    {
        _mockChatRepo = new Mock<IChatRepository>();
        _mockOpenAI = new Mock<IOpenAIService>();
        _chatService = new ChatService(_mockChatRepo.Object, _mockOpenAI.Object);
    }

    [Fact]
    public async Task CreateChat_ShouldReturnChatDto_WhenSuccessful()
    {
        // Arrange
        var userId = "user123";
        var message = "Hello AI";
        var expectedChat = new Chat { Id = Guid.NewGuid(), Title = "New Chat" };

        _mockChatRepo
            .Setup(x => x.CreateChatAsync(userId, message))
            .ReturnsAsync(expectedChat);

        // Act
        var result = await _chatService.CreateChatAsync(userId, message);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedChat.Id);
        result.Title.Should().Be("New Chat");
    }
}
```

### Integration Tests

**Uses Testcontainers for realistic testing:**

```csharp
public class ChatControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ChatControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace production dependencies with test containers
                services.AddTestcontainers();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateChat_ReturnsSuccess_WithValidToken()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateChatRequest
        {
            Message = "Tell me about semantic caching"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chat = await response.Content.ReadFromJsonAsync<ChatDto>();
        chat.Should().NotBeNull();
        chat.Title.Should().NotBeNullOrEmpty();
    }
}
```

**Testcontainers Setup:**

```csharp
services.AddSingleton<IContainer>(sp =>
{
    return new ContainerBuilder()
        .WithImage("mysql:8.0")
        .WithPortBinding(3306, true)
        .WithEnvironment("MYSQL_ROOT_PASSWORD", "testpass")
        .WithEnvironment("MYSQL_DATABASE", "test_db")
        .Build();
});
```

### Running Tests

**All tests:**

```bash
dotnet test
```

**Specific project:**

```bash
dotnet test backend.UnitTests
dotnet test backend.IntegrationTests
```

**With coverage:**

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

**Filter by category:**

```bash
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
```

**Verbose output:**

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Test Coverage

Current coverage areas:

- Authentication flow (register, login, OAuth)
- Chat creation and management
- Message operations (star, report)
- Semantic caching logic
- News aggregation and filtering
- Rate limiting enforcement

### Mocking External Services

**OpenAI:**

```csharp
_mockOpenAI
    .Setup(x => x.GetChatCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("Mocked AI response");
```

**Redis:**

```csharp
_mockRedis
    .Setup(x => x.GetAsync(It.IsAny<string>()))
    .ReturnsAsync((string)null);  // Cache miss
```

**MCP Client:**

```csharp
_mockMcp
    .Setup(x => x.ExecuteToolAsync("github_list_releases", It.IsAny<object>()))
    .ReturnsAsync(new[] { /* mock release data */ });
```

## 💻 Development

### Project Conventions

**Naming:**

- Controllers: `{Resource}Controller` (e.g., `ChatController`)
- Services: `{Domain}Service` (e.g., `ChatService`)
- Repositories: `{Entity}Repository` (e.g., `ChatRepository`)
- DTOs: `{Action}{Resource}Dto` (e.g., `CreateChatDto`)
- Interfaces: `I{ClassName}` (e.g., `IChatService`)

**Code Style:**

- Use `sealed` records for DTOs (immutable, performance)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use `var` for local variables when type is obvious
- Async methods: suffix with `Async`
- Use dependency injection for all services

**File Organization:**

- Group by feature/domain, not by type
- Keep related files together
- Use `Classes/` and `Interfaces/` subfolders in `Services/`, `Repository/`, etc.

### Adding New Features

**Example: Adding a "Favorite Chats" feature**

1. **Create DTO:**

```csharp
// Models/Dtos/Chat/FavoriteChatDto.cs
public sealed record FavoriteChatDto(
    Guid ChatId,
    bool IsFavorite
);
```

2. **Update Entity:**

```csharp
// Models/Domain/Chat.cs
public class Chat
{
    // ... existing properties
    public bool IsFavorite { get; set; }
}
```

3. **Create Migration:**

```bash
dotnet ef migrations add AddFavoriteToChat
dotnet ef database update
```

4. **Add Repository Method:**

```csharp
// Repository/Interfaces/IChatRepository.cs
Task<bool> ToggleFavoriteAsync(Guid chatId, string userId);

// Repository/Classes/ChatRepository.cs
public async Task<bool> ToggleFavoriteAsync(Guid chatId, string userId)
{
    var chat = await _context.Chats
        .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);

    if (chat is null) return false;

    chat.IsFavorite = !chat.IsFavorite;
    await _context.SaveChangesAsync();
    return true;
}
```

5. **Add Service Method:**

```csharp
// Services/Interfaces/IChatService.cs
Task<FavoriteChatDto> ToggleFavoriteAsync(Guid chatId, string userId);

// Services/Classes/ChatService.cs
public async Task<FavoriteChatDto> ToggleFavoriteAsync(Guid chatId, string userId)
{
    var success = await _chatRepo.ToggleFavoriteAsync(chatId, userId);
    if (!success) throw new NotFoundException("Chat not found");

    // Invalidate cache
    await _chatCache.InvalidateAsync(userId);

    return new FavoriteChatDto(chatId, true);
}
```

6. **Add Controller Endpoint:**

```csharp
// Controllers/ChatController.cs
[HttpPatch("{chatId:guid}/favorite")]
[EnableRateLimiting("ChatRateLimit")]
public async Task<IActionResult> ToggleFavorite(Guid chatId)
{
    var userId = User.GetUserId();
    var result = await _chatService.ToggleFavoriteAsync(chatId, userId);
    return Ok(result);
}
```

7. **Add Tests:**

```csharp
// backend.UnitTests/Services/ChatServiceTests.cs
[Fact]
public async Task ToggleFavorite_ShouldReturnTrue_WhenChatExists()
{
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";

    _mockChatRepo
        .Setup(x => x.ToggleFavoriteAsync(chatId, userId))
        .ReturnsAsync(true);

    // Act
    var result = await _chatService.ToggleFavoriteAsync(chatId, userId);

    // Assert
    result.IsFavorite.Should().BeTrue();
}
```

### Service Registration

**Add to appropriate extension file:**

```csharp
// Extensions/Services/ServicesConfiguration.cs
public static IServiceCollection AddBusinessServices(this IServiceCollection services)
{
    // ... existing registrations
    services.AddScoped<INewService, NewService>();
    return services;
}
```

**Lifetime Guidelines:**

- **Singleton**: Stateless services, caching (e.g., `LLMCacheService`)
- **Scoped**: Per-request services (e.g., `ChatService`, repositories)
- **Transient**: Lightweight, stateless utilities (rarely used)

### Database Migrations

**Workflow:**

1. Modify entity model
2. Create migration: `dotnet ef migrations add DescriptiveName`
3. Review generated migration in `Migrations/` folder
4. Test migration: `dotnet ef database update`
5. If incorrect, remove: `dotnet ef migrations remove`
6. Commit migration files to source control

**Migration Tips:**

- Use descriptive names: `AddFavoriteToChat`, not `Update1`
- Review SQL in `Up()` and `Down()` methods
- Test rollback: `dotnet ef database update PreviousMigration`
- Never modify applied migrations (create new one instead)

### Debugging Tips

**Enable detailed logging:**

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**Debug SignalR:**

```csharp
// Program.cs
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;  // Development only!
});
```

**Test MCP tools directly:**

```bash
# List available tools
curl http://localhost:8080/mcp/tools

# Execute tool
curl -X POST http://localhost:8080/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{"tool":"github_list_releases","parameters":{"repository":"microsoft/semantic-kernel"}}'
```

**Redis debugging:**

```bash
# Connect to Redis CLI
docker exec -it redis-stack redis-cli

# List all keys
KEYS *

# Get cache value
GET llm_cache:abc123

# Monitor operations
MONITOR
```

## 🐛 Troubleshooting

### Common Issues

#### 1. Database Connection Errors

**Error:**

```
MySql.Data.MySqlClient.MySqlException: Unable to connect to any of the specified MySQL hosts.
```

**Solutions:**

- Verify MySQL is running: `mysql -u root -p`
- Check connection string in `appsettings.json`
- Ensure database exists: `CREATE DATABASE ai_track_db;`
- Test connection pooling: reduce `MaxPoolSize` to 10 for testing

---

#### 2. Redis Connection Failures

**Error:**

```
StackExchange.Redis.RedisConnectionException: It was not possible to connect to the redis server(s).
```

**Solutions:**

- Verify Redis Stack is running: `docker ps | grep redis-stack`
- Test connection: `docker exec -it redis-stack redis-cli PING` (should return `PONG`)
- Check Redis configuration in appsettings
- Restart Redis: `docker restart redis-stack`

---

#### 3. MCP Server Not Found

**Error:**

```
HttpRequestException: No connection could be made because the target machine actively refused it.
```

**Solutions:**

- **GitHub MCP**: Verify Docker container is running: `docker ps | grep mcp-github`
- **Tavily MCP**: Ensure npx process is running
- Check MCP server logs: `docker logs mcp-github`
- Verify token configuration in `McpSettings`

---

#### 4. OpenAI API Rate Limits

**Error:**

```
OpenAI.OpenAIException: Rate limit exceeded
```

**Solutions:**

- Check your OpenAI usage dashboard
- Implement exponential backoff in `OpenAIService`
- Reduce request frequency in background services
- Upgrade OpenAI plan if needed
- Verify semantic caching is working (should reduce calls by 40-60%)

---

#### 5. Migration Failures

**Error:**

```
The migration '20250101000000_MigrationName' has already been applied to the database.
```

**Solutions:**

- Check applied migrations: `dotnet ef migrations list`
- Rollback: `dotnet ef database update PreviousMigration`
- If corrupted, delete database and reapply all: `dotnet ef database update`

---

#### 6. SignalR Connection Issues

**Error:**

```
Failed to start the connection: Error: WebSocket failed to connect.
```

**Solutions:**

- Verify CORS configuration includes `AllowCredentials()`
- Check frontend SignalR URL matches backend
- Enable SignalR detailed errors in development
- Test with Postman SignalR extension
- Check browser console for CORS errors

---

#### 7. JWT Token Validation Errors

**Error:**

```
Microsoft.IdentityModel.Tokens.SecurityTokenException: IDX10205: Issuer validation failed.
```

**Solutions:**

- Verify `JwtSettings:Issuer` matches in token and validation
- Ensure `JwtSettings:Key` is at least 32 characters
- Check token expiration (`exp` claim)
- Clear browser cookies and re-login
- Inspect token at jwt.io

---

#### 8. Chat Limit Exceeded

**Error:**

```
{ "error": "Chat limit exceeded. Maximum 10 chats allowed per user." }
```

**Solutions:**

- Delete old chats: `DELETE /chat/{chatId}`
- Increase limit in `MaxChatsAttribute` (not recommended for production)
- Implement chat archiving feature

---

#### 9. Background Service Not Running

**Symptom:** News not being collected daily

**Solutions:**

- Check logs for service startup
- Verify `NewsAggregationService` is registered: `services.AddHostedService<NewsAggregationService>()`
- Test manual trigger in development
- Check for unhandled exceptions in service
- Verify MCP clients initialized successfully

---

#### 10. n8n Webhook Failures

**Error:**

```
HttpRequestException: The SSL connection could not be established.
```

**Solutions:**

- For development, disable SSL validation (not for production!)
- Verify webhook URL in `N8N:NewsletterWebhookUrl`
- Test webhook manually with curl
- Check n8n logs for errors
- Verify API key is correct

---

### Logging

**View logs:**

**Console (Development):**
Logs appear in console during `dotnet run`

**MySQL (Production):**
Logs are persisted to MySQL via Serilog

Query logs:

```sql
SELECT * FROM Logs
WHERE Level = 'Error'
ORDER BY Timestamp DESC
LIMIT 50;
```

**Filter by context:**

```sql
SELECT * FROM Logs
WHERE Message LIKE '%ChatService%'
ORDER BY Timestamp DESC;
```

---

### Performance Profiling

**Enable detailed EF Core logging:**

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)  // Development only
           .EnableSensitiveDataLogging();  // Development only
});
```

**Profile Redis operations:**

```bash
docker exec -it redis-stack redis-cli --latency
```

**Monitor API performance:**
Use `dotnet-counters`:

```bash
dotnet-counters monitor --process-id <pid> --counters System.Runtime,Microsoft.AspNetCore.Hosting
```

---

### Reset Development Environment

**Complete reset:**

```bash
# Stop all services
docker stop redis-stack mcp-github

# Drop database
mysql -u root -p -e "DROP DATABASE ai_track_db; CREATE DATABASE ai_track_db;"

# Reapply migrations
cd backend
dotnet ef database update

# Clear Redis
docker exec -it redis-stack redis-cli FLUSHALL

# Restart containers
docker restart redis-stack mcp-github

# Run application
dotnet run
```

## 📄 License

This project is licensed under the MIT License. See the LICENSE file for details.

## 🙏 Acknowledgments

This project leverages amazing open-source technologies and services:

- **[OpenAI](https://openai.com/)** - GPT models and embedding API
- **[Model Context Protocol](https://modelcontextprotocol.io/)** - MCP SDK and specification
- **[Microsoft .NET](https://dotnet.microsoft.com/)** - Framework and runtime
- **[Redis Stack](https://redis.io/docs/stack/)** - Advanced caching and vector search
- **[SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr)** - Real-time communication
- **[Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)** - ORM and migrations
- **[MySQL](https://www.mysql.com/)** - Reliable database
- **[Tavily](https://tavily.com/)** - AI-optimized web search
- **[n8n](https://n8n.io/)** - Workflow automation platform
- **[Serilog](https://serilog.net/)** - Structured logging
- **[AutoMapper](https://automapper.org/)** - Object mapping
- **[xUnit](https://xunit.net/)**, **[FluentAssertions](https://fluentassertions.com/)**, **[Moq](https://github.com/moq/moq4)** - Testing frameworks
- **[Testcontainers](https://dotnet.testcontainers.org/)** - Integration testing with containers
