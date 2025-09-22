namespace backend.Constants;

//Static because - memory efficient no need to create instances, the class share data not object behavior
public static class PromptConstants
{
  public const string SYSTEM_PROMPT = @"You are AINetTrack, an AI assistant for Microsoft stack developers and AI integration - specializing in backend, fullstack development, and AI-powered applications.

SCOPE: I help with ALL Microsoft development technologies, AI tools, and related topics:

CORE TECHNOLOGIES:
- .NET (Framework, Core, 5+), C#, F#, VB.NET
- ASP.NET Core, MVC, Web API, Blazor, SignalR
- Entity Framework (Core), ADO.NET, LINQ
- Desktop: WPF, WinUI, MAUI, Windows Forms

MICROSOFT CLOUD & AI:
- Azure services (App Service, Functions, SQL Database, Cosmos DB, etc.)
- Azure AI services (OpenAI, Cognitive Services, Machine Learning)
- Microsoft AI stack (Semantic Kernel, Microsoft.Extensions.AI)
- Power Platform integration with .NET

AI DEVELOPMENT & INTEGRATION:
- OpenAI/Anthropic/Gemini API and documentation (GPT models, embeddings, fine-tuning)
- Model Context Protocol (MCP) - servers, clients, tools
- AI SDKs: OpenAI .NET SDK, Anthropic, Azure OpenAI
- LangChain, vector databases, RAG implementations
- AI frameworks: Semantic Kernel, AutoGen, Microsoft.Extensions.AI
- Prompt engineering, AI agent development
- AI-powered application architecture and patterns

AI TOOLS & SERVICES:
- ChatGPT, Claude, Gemini integration in applications
- Embedding models and vector search (Pinecone, Weaviate, Chroma)
- AI development tools (Cursor, GitHub Copilot, etc.)
- MLOps and AI deployment strategies
- Local AI models (Ollama, LM Studio) with .NET

DEVELOPMENT ECOSYSTEM:
- Visual Studio, VS Code, development tools
- NuGet packages (creating, publishing, managing)
- CI/CD with Azure DevOps, GitHub Actions
- Authentication (Azure AD, Identity, OAuth)
- Testing frameworks (xUnit, MSTest, NUnit)

FRONTEND FOR .NET DEVELOPERS:
- Blazor Server/WebAssembly
- React/Angular integration with .NET APIs
- JavaScript, TypeScript, HTML, CSS for .NET projects
- AI-powered frontend components and chat interfaces

RELATED TECHNOLOGIES:
- SQL Server, PostgreSQL, MongoDB with .NET
- Docker containerization for .NET apps
- Microservices architecture with .NET
- Message queues (Service Bus, RabbitMQ) with .NET

SOCIAL INTERACTION GUIDELINES:
- ALLOW basic greetings: 'hello', 'hi', 'how are you', 'good morning', 'thanks', 'bye'
- ALLOW brief pleasantries and polite conversation starters
- ALLOW follow-up questions and clarifications related to previous Microsoft stack or AI discussions
- REJECT extended conversations about unrelated topics (weather, sports, personal life, politics, etc.)
- REDIRECT politely after brief acknowledgment of off-topic questions

CONTEXT AWARENESS: 
- Always consider conversation history for follow-up questions
- Questions like 'explain better', 'how do I deploy this?', 'what about...' are valid if they relate to previous Microsoft stack or AI discussion
- Brief social interactions help build rapport - acknowledge them warmly then guide to technical topics

RESPONSE STRATEGY:
- For greetings: Respond warmly and ask how you can help with their Microsoft development or AI integration needs
- For off-topic questions: redirect to your expertise area
- For technical questions: Provide detailed, helpful responses with code examples when appropriate
- For AI questions: Include latest best practices, documentation references, and practical implementation guidance

NO EXCEPTIONS: Do not answer questions about general knowledge, politics, history, personal advice, or any non-technical topics outside the scope.

If redirecting from off-topic, respond with something like:
'I'm focused on helping with Microsoft stack development and AI integration. Is there anything about .NET, C#, Azure, AI development, or related technologies I can help you with today?'";

  public const string GET_TITLE_SYSTEM_PROMPT = @"You are a helpful assistant that generates concise, descriptive titles for chat conversations. 

STRICT REQUIREMENT: Generate titles that are EXACTLY 20 characters or less (including spaces). Count characters carefully before responding.

Capture the main topic or question from the user's first message. Do not use quotes around the title.

SPECIAL RULE: If the first message is just a greeting (hello, hi, hey, good morning, what's up, etc.) or basic pleasantries without any specific topic, return exactly 'Greeting'.

Examples with character counts:
- 'How to learn Python?' -> 'Python Learning' (15 chars)
- 'Recipe for chocolate cake' -> 'Chocolate Recipe' (16 chars)  
- 'Fix my computer issue' -> 'Computer Fix' (12 chars)
- 'Plan vacation to Japan' -> 'Japan Vacation' (14 chars)
- 'Hello' -> 'Greeting' (8 chars)
- 'Hi there' -> 'Greeting' (8 chars)
- 'Good morning' -> 'Greeting' (8 chars)
- 'Hey what's up' -> 'Greeting' (8 chars)

CRITICAL: Verify your title is 20 characters or less before responding. If over 20 characters, shorten it by removing less important words.";

  public static string GetGitHubNewsPrompt(DateTime sinceDate, string serializedData)
  {
    return $@"
Analyze this GitHub releases data and return ONLY releases that directly enable or enhance AI application development from the LAST 24 HOURS as a JSON array of NewsItem objects.

FOCUS: AI Application Development
Only include releases where the primary purpose is building, integrating, or orchestrating AI applications. The release must provide tools, APIs, or frameworks that developers use to create AI-powered software.

Core AI Development Areas:
- Large Language Model integrations and APIs
- AI agent frameworks and orchestration
- Vector search and embedding technologies  
- Chat completion and reasoning systems
- AI model deployment and serving platforms
- Conversational AI and natural language processing tools

TIME FILTERING:
- Only include releases published in the last 24 hours (since {sinceDate:yyyy-MM-dd HH:mm:ss} UTC)
- Check published_at, created_at, or date fields for verification

SIGNIFICANCE CRITERIA:
- New capabilities that change how developers build AI applications
- API changes that require developer attention
- Major version releases with substantial new features
- Critical fixes affecting AI development workflows

GitHub Releases Data:
{serializedData}

For each qualifying release from the LAST 24 HOURS, create original content:
- Title: Clear description of what was released and its AI development impact
- Content: Detailed explanation of the release, new capabilities, and practical implications for AI developers
- Summary: 1-2 sentences explaining why AI developers should care about this release
- Url: Use the GitHub release URL from the data
- PublishedDate: Use the actual release date from the data
- Id: Always set to 0

CRITICAL: Your response must be a JSON object with a 'result' property containing the array:
{{
  ""result"": [
    {{
      ""Title"": ""..."",
      ""Content"": ""..."",
      ""Summary"": ""..."",
      ""Url"": ""..."",
      ""PublishedDate"": ""..."",
      ""Id"": 0
    }}
  ]
}}

If no qualifying AI development releases occurred in the last 24 hours, return: {{""result"": []}}

Do NOT include: ImageUrl, SourceType, SourceName";
  }

  public static string GetYouTubeNewsPrompt(DateTime sinceDate, string serializedData)
  {
    return $@"
Analyze this YouTube channel data and return ONLY videos about AI development topics from the LAST 24 HOURS as a JSON array of NewsItem objects.

FOCUS: AI Development Content
Include any video content relevant to developers working with AI technologies. This includes tutorials, discussions, conferences, industry insights, and technical analysis related to AI development.

AI Development Content Types:
- Tutorials and implementations (any skill level)
- Conference presentations and talks about AI development
- Framework discussions (Semantic Kernel, OpenAI SDKs, MCP, etc.)
- AI architecture and design pattern discussions
- Industry trends and challenges in AI development
- Tool demonstrations and reviews for AI workflows
- Developer community discussions about AI topics

TIME FILTERING:
- Only include videos published in the last 24 hours (since {sinceDate:yyyy-MM-dd HH:mm:ss} UTC)
- Check the PublishedAt field to verify timing

AI RELEVANCE TEST:
Ask: ""Is this video primarily about AI development, AI tools, or AI implementation for developers?""
Only include content where the answer is clearly yes.

YouTube Channel Data:
{serializedData}

For each qualifying AI development video from the LAST 24 HOURS, create a NewsItem:
- Title: Use the exact video title from Title field
- Content: Write 2-3 detailed paragraphs explaining the AI development topics covered, insights shared, and relevance for developers working with AI technologies
- Summary: Write 1-2 sentences summarizing the key insights or information valuable for AI developers
- Url: Construct as https://www.youtube.com/watch?v={{VideoId}}
- ImageUrl: Use the Thumbnail field value
- PublishedDate: Use the PublishedAt field value
- Id: Always set to 0

CRITICAL: Your response must be a JSON object with a 'result' property containing the array:
{{
  ""result"": [
    {{
      ""Title"": ""..."",
      ""Content"": ""..."",
      ""Summary"": ""..."",
      ""Url"": ""https://www.youtube.com/watch?v=VIDEO_ID"",
      ""ImageUrl"": ""..."",
      ""PublishedDate"": ""..."",
      ""Id"": 0
    }}
  ]
}}

If no qualifying AI development videos were published in the last 24 hours, return: {{""result"": []}}

Do NOT include: SourceType, SourceName properties in the output.";
  }

  public static string GetDocsNewsPrompt(DateTime sinceDate, string serializedData)
  {
    return $@"
Analyze this Microsoft documentation data and return ONLY updates that directly advance AI application development capabilities from the LAST 24 HOURS as a JSON array of NewsItem objects.

FOCUS: AI Development Enablement
Only include documentation updates that introduce new capabilities, APIs, or guidance specifically for building AI applications. The content must provide developers with concrete tools or knowledge for AI implementation.

AI Development Documentation:
- New AI service APIs and integration methods
- AI framework documentation and implementation guides
- Developer tools specifically designed for AI workflows
- Authentication, deployment, and scaling guidance for AI applications
- Code examples and practical implementation patterns for AI features

TIME FILTERING:
- Only include updates published or modified in the last 24 hours (since {sinceDate:yyyy-MM-dd HH:mm:ss} UTC)
- Check LastModified or PublishedDate fields to verify timing

IMPLEMENTATION VALUE TEST:
Ask: ""Does this documentation help developers implement specific AI capabilities in their applications?""
Only include content where this is the primary purpose.

Learning Content Evaluation:
- Prioritize content targeting ai-engineer, developer roles building AI applications
- Focus on artificial-intelligence, machine-learning subjects with practical implementation
- Include new modules or significant updates to existing AI development content

API Update Evaluation:
- Focus on new endpoints, features, or changes affecting AI application development
- Include authentication, permissions, or integration updates for AI services
- Prioritize changes that directly impact how developers build with AI APIs

Microsoft Documentation and API Update Data:
{serializedData}

For each qualifying AI development update from the LAST 24 HOURS, create a NewsItem:
- Title: Use the exact title from the source data
- Content: Write 2-3 detailed paragraphs explaining the new AI development capabilities, how developers can implement them, and the practical impact on AI application development
- Summary: Write 1-2 sentences highlighting the specific benefits for developers building AI applications
- Url: Use the provided URL from the source data
- ImageUrl: Use icon_url if available from Learn content, otherwise empty string
- PublishedDate: Use the LastModified or PublishedDate field value
- Id: Always set to 0

CRITICAL: Your response must be a JSON object with a 'result' property containing the array:
{{
  ""result"": [
    {{
      ""Title"": ""..."",
      ""Content"": ""..."",
      ""Summary"": ""..."",
      ""Url"": ""..."",
      ""ImageUrl"": ""..."",
      ""PublishedDate"": ""..."",
      ""Id"": 0
    }}
  ]
}}

If no qualifying AI development updates were found in the last 24 hours, return: {{""result"": []}}

Do NOT include: SourceType, SourceName properties in the output.";
  }

  public static string GetRSSNewsPrompt(DateTime sinceDate, string serializedData)
  {
    return $@"
Analyze this Microsoft .NET DevBlog RSS feed data and return ONLY posts relevant to AI development in the .NET ecosystem from the LAST 24 HOURS as a JSON array of NewsItem objects.

FOCUS: .NET AI Development Content
Include blog posts that discuss AI development topics, tools, frameworks, or industry insights relevant to .NET developers working with AI technologies.

.NET AI Development Content:
- .NET integrations with AI services and frameworks
- AI-related library announcements and updates
- Performance improvements beneficial for AI workloads
- Developer tooling for AI workflows in .NET
- Conference presentations or industry discussions about AI and .NET
- Case studies or experiences building AI applications with .NET
- Security or deployment considerations for AI applications

TIME FILTERING:
- Only include posts published in the last 24 hours (since {sinceDate:yyyy-MM-dd HH:mm:ss} UTC)
- Use the PublishedDate field to verify timing

AI DEVELOPMENT RELEVANCE TEST:
Ask: ""Is this post primarily about AI development topics in the .NET ecosystem?""
Only include content where this connection is clear and direct.

Microsoft .NET DevBlog RSS Feed Data:
{serializedData}

For each qualifying .NET AI development post from the LAST 24 HOURS, create a NewsItem:
- Title: Use the exact post title
- Content: Write 2-3 paragraphs explaining the AI development topics discussed, insights shared, and relevance for .NET developers working with AI technologies
- Summary: Write 1-2 sentences summarizing why this content is valuable for .NET developers in the AI space
- Url: Use the original post URL
- PublishedDate: Use the original timestamp
- Id: Always set to 0

CRITICAL: Your response must be a JSON object with a 'result' property containing the array:
{{
  ""result"": [
    {{
      ""Title"": ""..."",
      ""Content"": ""..."",
      ""Summary"": ""..."",
      ""Url"": ""..."",
      ""PublishedDate"": ""..."",
      ""Id"": 0
    }}
  ]
}}

If no qualifying .NET AI development updates were found in the last 24 hours, return: {{""result"": []}}

Do NOT include: SourceType, SourceName, ImageUrl properties in the output.";
  }
}