# AI Track - Frontend

A modern, high-performance **React 19** application that serves as the interactive user interface for the AI Track system. Built with **Vite**, this frontend delivers a seamless real-time chat experience powered by **SignalR**, intelligent news aggregation through an infinite-scroll timeline, and a sophisticated state management architecture combining **Redux Toolkit** and **TanStack Query**.

## Table of Contents

- [Key Features](#-key-features)
- [Tech Stack](#-tech-stack)
- [Architecture Overview](#-architecture-overview)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
- [Configuration Guide](#-configuration-guide)
- [Features Deep Dive](#-features-deep-dive)
- [Project Structure](#-project-structure)
- [State Management](#-state-management)
- [API Integration](#-api-integration)
- [Custom Hooks](#-custom-hooks)
- [Component Patterns](#-component-patterns)
- [Development](#-development)
- [Troubleshooting](#-troubleshooting)
- [Deployment](#-deployment)

## 🚀 Key Features

### Real-Time AI Chat with SignalR Streaming
- **Bidirectional WebSocket communication** via Microsoft SignalR for instant message delivery
- **Streaming AI responses** with chunk-by-chunk rendering for responsive UX
- **Tool usage indicators** showing when AI uses external tools (Tavily search, GitHub integration)
- **Stop generation capability** to cancel long-running AI responses
- **Automatic reconnection** with exponential backoff strategy
- **Message persistence** with full chat history access

### Rich Content Rendering
- **Markdown support** with `react-markdown` for formatted text and tables
- **Syntax highlighting** for code blocks using `react-syntax-highlighter` with GitHub theme
- **GFM (GitHub Flavored Markdown)** support via `remark-gfm` for enhanced formatting
- **Copy-to-clipboard** functionality for code blocks with visual feedback
- **Streaming accumulation** that combines message chunks before final render

### News Timeline & Aggregation
- **Infinite scroll** news feed powered by `react-infinite-scroll-component`
- **Date-based filtering** with interactive date picker (5-day batch pagination)
- **News type filtering** (GitHub releases, RSS feeds, YouTube videos)
- **Debounced search** (500ms delay) for efficient server queries
- **Detailed news modals** with full content and source links
- **Chat-from-news** feature to discuss articles with AI

### Message Management
- **Star important messages** with instant optimistic updates
- **Report/feedback system** for problematic responses with reason tracking
- **Cross-chat starred messages** view for centralized access
- **Message actions** (copy, star, report) with visual feedback
- **Timezone-aware timestamps** for consistent time display

### Authentication & User Management
- **Email/password authentication** with JWT session management
- **Google OAuth2 integration** for seamless social login
- **Session persistence** via HttpOnly cookies
- **Auth status checking** on app initialization
- **Automatic SignalR connection** after successful authentication
- **Profile management** (email, password, full name updates)
- **Newsletter subscription** toggle
- **Account deletion** with confirmation flow

### Chat History & Sidebar
- **Collapsible sidebar** with chat list and user section
- **Chat CRUD operations** (create, rename, delete with confirmation)
- **Time-relative formatting** ("Just now", "5 min ago", "Yesterday")
- **Auto-refresh** on new messages with cache invalidation
- **Responsive design** with mobile-friendly sidebar toggle
- **Skeleton loading states** for improved perceived performance

### Advanced UI/UX Features
- **Auto-scroll to bottom** on new messages with smart scroll detection
- **Loading skeletons** for chat list and message history
- **Error popup notifications** with 5-second auto-dismiss
- **Rate limiting awareness** with specific error handling for 429 responses
- **Optimistic updates** for instant UI feedback before server confirmation
- **CSS Modules** with camelCase convention for scoped styling
- **Dark mode design** with custom CSS variables

## 🛠️ Tech Stack

### Core Framework
- **React 19.1.1**: Latest React with modern features and concurrent rendering
- **Vite 7.1.0**: Lightning-fast build tool with HMR and optimized bundling
- **JavaScript (ES6+)**: Modern JavaScript with modules

### Routing & Navigation
- **React Router DOM 7.8.0**: Declarative routing with nested routes and layouts

### State Management
- **Redux Toolkit 2.8.2**: Client-side state management (auth, UI state)
- **TanStack Query 5.84.2**: Server state management with caching and automatic refetching
- **React Redux 9.2.2**: React bindings for Redux

### Real-Time Communication
- **@microsoft/signalr 9.0.6**: WebSocket-based bidirectional communication for chat streaming

### UI Libraries & Content Rendering
- **react-markdown 10.1.0**: Markdown-to-React component parser
- **react-syntax-highlighter 15.6.1**: Syntax highlighting for code blocks
- **remark-gfm 4.0.1**: GitHub Flavored Markdown plugin for react-markdown
- **react-infinite-scroll-component 6.1.0**: Infinite scroll implementation for timeline

### Styling
- **CSS Modules**: Scoped CSS with automatic class name generation
- **Custom CSS Variables**: Consistent theming and dark mode support

### Development Tools
- **ESLint 9.32.0**: Code quality and style enforcement
- **@vitejs/plugin-react 4.3.4**: Official Vite plugin for React with Fast Refresh

## 🏗️ Architecture Overview

### Project Structure

```
frontend/
├── src/
│   ├── api/                    # API integration layer
│   │   ├── auth.js            # Authentication endpoints (login, register, logout)
│   │   ├── chat.js            # Chat CRUD operations
│   │   ├── chatHub.js         # SignalR singleton service (connection management)
│   │   ├── config.js          # API base URL and endpoint definitions
│   │   ├── messages.js        # Message operations (star, report)
│   │   ├── news.js            # News fetching and search
│   │   ├── user.js            # User profile updates
│   │   └── index.js           # API barrel exports
│   ├── components/            # Reusable UI components
│   │   ├── AccountSettings/   # Profile management sections
│   │   │   ├── DeleteAccount/ # Account deletion with confirmation
│   │   │   ├── EmailSection/  # Email update form
│   │   │   ├── Newsletter/    # Newsletter subscription toggle
│   │   │   ├── PasswordSection/ # Password change form
│   │   │   └── UsernameSection/ # Full name update form
│   │   ├── Auth/              # Authentication components
│   │   │   ├── Login/         # Login form with email/password
│   │   │   └── Signup/        # Registration form with validation
│   │   ├── Chat/              # Chat feature components
│   │   │   ├── ChatInput/     # Message input with send/cancel buttons
│   │   │   ├── InitialChat/   # Unauthenticated welcome screen
│   │   │   ├── MainChat/      # Authenticated chat container with SignalR
│   │   │   ├── MessageList/   # Message renderer with markdown and actions
│   │   │   └── TypingIndicator/ # Tool usage status display
│   │   ├── ErrorPopup/        # Global error notification component
│   │   ├── Header/            # Top navigation bar
│   │   ├── MarkdownRenderer/  # Markdown and code rendering component
│   │   ├── ReportModal/       # Feedback submission modal
│   │   ├── Sidebar/           # Chat history sidebar with user section
│   │   ├── Skeleton/          # Loading skeleton components
│   │   └── Timeline/          # News timeline feature
│   │       ├── Timeline.jsx   # Main timeline container
│   │       └── components/    # Timeline subcomponents
│   │           ├── DateSelector/  # Date picker for filtering
│   │           ├── NewsCard/      # Individual news item card
│   │           ├── NewsModal/     # Detailed news view modal
│   │           └── ... (other timeline components)
│   ├── contexts/              # React Context providers
│   │   └── AuthProvider.jsx   # Authentication initialization and SignalR setup
│   ├── hooks/                 # Custom React hooks
│   │   ├── useAccountSettings.js  # Profile form handling and submission
│   │   ├── useAutoScroll.js       # Auto-scroll to bottom logic
│   │   ├── useDatePagination.js   # Timeline date batch pagination
│   │   ├── useSignalRChat.js      # Core SignalR chat logic (243 lines)
│   │   ├── useSidebar.js          # Sidebar chat management and formatting
│   │   └── ... (other utility hooks)
│   ├── pages/                 # Route page components
│   │   ├── AccountSettingsPage/
│   │   ├── AuthCallback/      # Google OAuth2 callback handler
│   │   ├── ChatPage/          # Main chat page
│   │   ├── InitialChatPage/   # Landing page for unauthenticated users
│   │   ├── NotFound/          # 404 page
│   │   ├── StarredMessagesPage/ # Centralized starred messages view
│   │   └── TimelinePage/      # News timeline page
│   ├── router/                # React Router configuration
│   │   └── index.jsx          # Route definitions and protected routes
│   ├── store/                 # Redux state management
│   │   ├── index.js           # Store configuration and root reducer
│   │   ├── userAuth.js        # Authentication state slice
│   │   ├── chat.js            # Chat list state slice
│   │   ├── sidebarSlice.js    # Sidebar UI state (open/closed)
│   │   └── messagesSlice.js   # Messages state (starred/reported)
│   ├── utils/                 # Helper functions and DTOs
│   │   ├── auth.js            # Auth DTOs (UserInfo, LoginResponse)
│   │   └── validation.js      # Form validation utilities
│   ├── App.jsx                # Root component with routing
│   └── main.jsx               # Application entry point (Redux, Query, Router setup)
├── public/                    # Static assets
├── index.html                 # HTML template
├── vite.config.js            # Vite bundler configuration
├── package.json              # Dependencies and scripts
├── eslint.config.js          # ESLint configuration
└── README.md                 # This file
```

### Architecture Layers

**Clean Frontend Architecture with Separation of Concerns:**

1. **Presentation Layer** (`pages/`, `components/`)
   - Route pages for different app sections
   - Reusable UI components with props interface
   - CSS Modules for scoped styling

2. **Business Logic Layer** (`hooks/`)
   - Custom hooks encapsulating complex logic
   - Separation of UI from business rules
   - Reusable stateful logic across components

3. **Data Access Layer** (`api/`)
   - API service functions for HTTP requests
   - SignalR hub service for WebSocket communication
   - Centralized endpoint configuration

4. **State Management Layer** (`store/`, TanStack Query)
   - Redux for client-side UI state
   - TanStack Query for server state with caching
   - Clear separation of concerns

### Data Flow

```
User Interaction
    ↓
Component/Page
    ↓
Custom Hook (Business Logic)
    ↓
API Service Function
    ↓
HTTP Request OR SignalR Event
    ↓
Backend (ASP.NET Core)
    ↓
Response
    ↓
Redux Dispatch OR TanStack Query Cache Update
    ↓
Component Re-render
```

### SignalR Streaming Architecture

```
User sends message
    ↓
SignalR Hub.SendMessage(chatId, content)
    ↓
Backend processes with OpenAI streaming
    ↓
Backend emits chunks via Hub.ReceiveMessage
    ↓
ChatHubService receives chunks
    ↓
useSignalRChat accumulates chunks in local state
    ↓
MessageList renders accumulated content
    ↓
Backend sends final complete message
    ↓
useSignalRChat replaces chunks with complete message
    ↓
Final render with timestamps and metadata
```

### State Management Strategy

**Separation of Concerns:**

```
Redux (Client State):           TanStack Query (Server State):
┌─────────────────────┐        ┌──────────────────────────┐
│ • userAuth          │        │ • Chat fetching/caching  │
│ • chat (chat list)  │        │ • Message history        │
│ • sidebar (UI)      │        │ • News data              │
│ • messages (local)  │        │ • Search results         │
└─────────────────────┘        │ • Starred messages       │
                                └──────────────────────────┘
```

**When to use Redux:**
- Authentication state (isAuthenticated, user info)
- UI state (sidebar open/closed, theme)
- Cross-component communication (error popups)

**When to use TanStack Query:**
- Data fetching from backend
- Automatic caching and refetching
- Loading and error states
- Server state synchronization

## 📋 Prerequisites

Ensure you have the following installed:

- **[Node.js](https://nodejs.org/)** - Version 18+ recommended (v18.0.0 or higher)
- **npm** or **yarn** - Package manager (npm comes with Node.js)
- **Backend server** - The ASP.NET Core backend must be running (see backend README)

## 🏁 Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd "Ai Track/frontend"
```

### 2. Install Dependencies

```bash
npm install
```

Or with yarn:
```bash
yarn install
```

### 3. Configure Backend Connection

Update the API base URL in `src/api/config.js` if your backend is not running on the default port:

```javascript
// src/api/config.js
export const API_BASE_URL = "http://localhost:5170"; // Update if needed
```

**Note:** The default backend URL is `http://localhost:5170`. If you've configured the backend to run on a different port (e.g., `https://localhost:7197`), update this value accordingly.

### 4. Start Development Server

```bash
npm run dev
```

The application will be available at **`http://localhost:5173`** by default.

You should see output similar to:
```
  VITE v7.1.0  ready in 432 ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
  ➜  press h + enter to show help
```

### 5. Verify Setup

**Open your browser:**
Navigate to `http://localhost:5173`

**Check backend connection:**
- You should see the login/signup screen
- The app should not show connection errors
- Verify the backend is running at `http://localhost:5170` (or your configured URL)

**Test SignalR connection:**
- Sign up or log in
- Send a chat message
- You should see streaming AI responses

## ⚙️ Configuration Guide

### API Configuration

**Main configuration file:** `src/api/config.js`

```javascript
// Base URL for all API requests
export const API_BASE_URL = "http://localhost:5170";

// API Endpoints
export const API_ENDPOINTS = {
  // Authentication
  REGISTER: `${API_BASE_URL}/auth`,
  LOGIN: `${API_BASE_URL}/auth/login`,
  LOGOUT: `${API_BASE_URL}/auth/logout`,
  AUTH_STATUS: `${API_BASE_URL}/auth/status`,
  GOOGLE_LOGIN: `${API_BASE_URL}/auth/google-login`,

  // Chat operations
  CHATS: `${API_BASE_URL}/chat`,
  CHAT_BY_ID: (id) => `${API_BASE_URL}/chat/${id}`,
  CHAT_TITLE: (id) => `${API_BASE_URL}/chat/${id}/title`,

  // Messages
  STARRED_MESSAGES: `${API_BASE_URL}/messages/starred`,
  MESSAGE_STARRED: (id) => `${API_BASE_URL}/messages/${id}/starred`,
  MESSAGE_REPORT: (id) => `${API_BASE_URL}/messages/${id}/report`,

  // News
  NEWS: `${API_BASE_URL}/news`,
  NEWS_SEARCH: `${API_BASE_URL}/news/search`,

  // User profile
  UPDATE_EMAIL: `${API_BASE_URL}/profile/email`,
  UPDATE_USERNAME: `${API_BASE_URL}/profile/username`,
  UPDATE_PASSWORD: `${API_BASE_URL}/profile/password`,
  UPDATE_NEWSLETTER: `${API_BASE_URL}/profile/newsletter`,
  DELETE_ACCOUNT: `${API_BASE_URL}/profile`,
};

// SignalR Hub URL
export const CHAT_HUB_URL = `${API_BASE_URL}/chathub`;
```

### Environment Variables (Optional)

For different environments (development, staging, production), you can create environment-specific configuration:

**Create `.env` file in frontend root:**

```env
VITE_API_BASE_URL=http://localhost:5170
VITE_ENVIRONMENT=development
```

**Update `config.js` to use environment variables:**

```javascript
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5170";
```

**Environment-specific files:**
- `.env.development` - Development environment
- `.env.production` - Production environment
- `.env.staging` - Staging environment

### Vite Configuration

**File:** `vite.config.js`

```javascript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  css: {
    modules: {
      localsConvention: 'camelCase', // CSS class names converted to camelCase in JS
      scopeBehaviour: 'local',
    }
  },
  server: {
    port: 5173, // Development server port
    open: true, // Auto-open browser
    cors: true, // Enable CORS for API requests
  },
  build: {
    outDir: 'dist', // Output directory for production build
    sourcemap: false, // Disable source maps in production
    rollupOptions: {
      output: {
        manualChunks: {
          // Code splitting for better caching
          vendor: ['react', 'react-dom', 'react-router-dom'],
          redux: ['@reduxjs/toolkit', 'react-redux'],
          query: ['@tanstack/react-query'],
          signalr: ['@microsoft/signalr'],
          markdown: ['react-markdown', 'react-syntax-highlighter', 'remark-gfm'],
        }
      }
    }
  }
})
```

### TanStack Query Configuration

**File:** `src/main.jsx`

```javascript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,     // Data considered fresh for 5 minutes
      cacheTime: 10 * 60 * 1000,    // Cache kept for 10 minutes
      retry: 1,                      // Retry failed requests once
      refetchOnWindowFocus: false,   // Don't refetch when window regains focus
    },
  },
});
```

**Configuration options explained:**
- **staleTime**: How long data is considered fresh (5 minutes)
- **cacheTime**: How long unused data stays in cache (10 minutes)
- **retry**: Number of retry attempts for failed requests (1)
- **refetchOnWindowFocus**: Whether to refetch when tab becomes active (disabled)

### Redux Store Configuration

**File:** `src/store/index.js`

```javascript
import { configureStore } from '@reduxjs/toolkit';
import userAuthReducer from './userAuth';
import chatReducer from './chat';
import sidebarReducer from './sidebarSlice';
import messagesReducer from './messagesSlice';

export const store = configureStore({
  reducer: {
    userAuth: userAuthReducer,
    chat: chatReducer,
    sidebar: sidebarReducer,
    messages: messagesReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false, // Disable for Date objects
    }),
});
```

## 🔍 Features Deep Dive

### Real-Time Chat System

The chat system leverages **SignalR** for bidirectional WebSocket communication, enabling real-time streaming of AI responses with sub-second latency.

#### Architecture Components

**1. ChatHubService (Singleton Pattern)**

Located in `src/api/chatHub.js`, this is the core SignalR connection manager:

```javascript
class ChatHubService {
  constructor() {
    if (ChatHubService.instance) {
      return ChatHubService.instance;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(CHAT_HUB_URL, {
        withCredentials: true, // Include auth cookies
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // Reconnect strategy
      .build();

    ChatHubService.instance = this;
  }

  // Event handlers
  onReceiveMessage(callback) { /* ... */ }
  onChatJoined(callback) { /* ... */ }
  onError(callback) { /* ... */ }

  // Hub methods
  async joinChat(chatId) { /* ... */ }
  async sendMessage(chatId, content) { /* ... */ }
  async stopGeneration(chatId) { /* ... */ }
}
```

**Key features:**
- **Singleton pattern**: One connection shared across entire app
- **Automatic reconnection**: Exponential backoff (0ms, 2s, 5s, 10s)
- **Credential support**: Includes HttpOnly cookies for authentication
- **Event-driven**: Subscribe to `ReceiveMessage`, `ChatJoined`, `Error` events

**2. useSignalRChat Hook**

Located in `src/hooks/useSignalRChat.js` (243 lines), this hook manages chat state and SignalR interactions:

```javascript
const useSignalRChat = (chatId) => {
  const [messages, setMessages] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [accumulatedChunks, setAccumulatedChunks] = useState({});

  useEffect(() => {
    // Subscribe to SignalR events
    const handleReceiveMessage = (message) => {
      if (message.isChunk) {
        // Accumulate streaming chunks
        setAccumulatedChunks(prev => ({
          ...prev,
          [message.id]: (prev[message.id] || '') + message.content
        }));
      } else {
        // Final complete message
        setMessages(prev => [...prev, message]);
        setAccumulatedChunks(prev => {
          const { [message.id]: removed, ...rest } = prev;
          return rest;
        });
      }
    };

    chatHubService.onReceiveMessage(handleReceiveMessage);

    // Cleanup on unmount
    return () => {
      chatHubService.offReceiveMessage(handleReceiveMessage);
    };
  }, [chatId]);

  const sendMessage = async (content) => {
    setIsLoading(true);
    try {
      await chatHubService.sendMessage(chatId, content);
    } catch (error) {
      console.error('Send message error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const stopGeneration = async () => {
    await chatHubService.stopGeneration(chatId);
  };

  return { messages, isLoading, sendMessage, stopGeneration, accumulatedChunks };
};
```

**Features:**
- **Chunk accumulation**: Combines streaming chunks in local state
- **Message ID tracking**: Uses temporary IDs (`temp-{timestamp}`) during typing
- **Error recovery**: Removes incomplete messages on error
- **Stop generation**: Cancels long-running AI responses
- **Cleanup**: Properly unsubscribes from events on unmount

**3. Message Flow**

```
User types message → ChatInput component
    ↓
User clicks Send → sendMessage() called
    ↓
SignalR: Hub.SendMessage(chatId, content)
    ↓
Backend receives → Processes with OpenAI
    ↓
Backend streams chunks → Hub.ReceiveMessage({ isChunk: true, content: "..." })
    ↓
Frontend accumulates chunks → setAccumulatedChunks()
    ↓
MessageList renders accumulated content → Live updating
    ↓
Backend sends final message → Hub.ReceiveMessage({ isChunk: false, ...fullMessage })
    ↓
Frontend replaces chunks with final message → setMessages()
    ↓
Display complete message with timestamp and metadata
```

**4. Tool Usage Indicators**

The chat system detects when the AI uses external tools (Tavily search, GitHub integration) and displays user-friendly indicators:

```javascript
// Tool markers sent by backend
const TOOL_MARKERS = {
  'tavily-search': 'Searching the internet...',
  'get_file_contents': 'Reading GitHub file...',
  'list_files': 'Browsing GitHub repository...',
  'search_code': 'Searching code on GitHub...',
};

// TypingIndicator component
const getToolMessage = (toolName) => {
  return TOOL_MARKERS[toolName] || `Using ${toolName}...`;
};
```

**5. Connection State Management**

```javascript
// AuthProvider.jsx - Connects SignalR after authentication
useEffect(() => {
  if (isAuthenticated) {
    chatHubService.start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('SignalR connection failed:', err));
  } else {
    chatHubService.stop()
      .then(() => console.log('SignalR disconnected'));
  }
}, [isAuthenticated]);
```

---

### Timeline & News Feature

The Timeline provides an infinite-scroll news feed with advanced filtering and search capabilities.

#### Key Components

**1. Timeline Container** (`src/components/Timeline/Timeline.jsx`)

Main features:
- **Date-based pagination**: Loads news in 5-day batches
- **Infinite scroll**: Automatically loads more as user scrolls
- **Multiple filters**: Date range, news type (GitHub/RSS/YouTube)
- **Debounced search**: 500ms delay to reduce server load
- **Modal view**: Detailed news display with "Chat with AI" button

**2. Date Pagination Hook** (`src/hooks/useDatePagination.js`)

```javascript
const useDatePagination = (initialDate = new Date()) => {
  const [currentDate, setCurrentDate] = useState(initialDate);
  const [dateRanges, setDateRanges] = useState([]);

  const loadNextBatch = () => {
    const startDate = currentDate;
    const endDate = new Date(currentDate);
    endDate.setDate(endDate.getDate() - 5); // 5-day batches

    setDateRanges(prev => [...prev, { startDate, endDate }]);
    setCurrentDate(endDate);
  };

  return { dateRanges, loadNextBatch, hasMore: true };
};
```

**3. News Fetching with TanStack Query**

```javascript
// src/api/news.js
export const fetchNews = async (params) => {
  const { dateTime, newsType, startDate, endDate, searchTerm } = params;

  let url = API_ENDPOINTS.NEWS;
  const queryParams = new URLSearchParams();

  if (searchTerm) {
    url = API_ENDPOINTS.NEWS_SEARCH;
    queryParams.append('searchTerm', searchTerm);
  } else {
    if (dateTime) queryParams.append('dateTime', dateTime.toISOString());
    if (newsType) queryParams.append('newsType', newsType);
    if (startDate) queryParams.append('startDate', startDate.toISOString());
    if (endDate) queryParams.append('endDate', endDate.toISOString());
  }

  const response = await fetch(`${url}?${queryParams}`, {
    credentials: 'include', // Include auth cookies
  });

  if (!response.ok) throw new Error('Failed to fetch news');
  return response.json();
};
```

**4. Search with Debouncing**

```javascript
// Timeline component
const [searchTerm, setSearchTerm] = useState('');
const [debouncedSearch, setDebouncedSearch] = useState('');

useEffect(() => {
  const timer = setTimeout(() => {
    setDebouncedSearch(searchTerm);
  }, 500); // Wait 500ms after user stops typing

  return () => clearTimeout(timer);
}, [searchTerm]);

// Use debouncedSearch for API calls
const { data: newsResults } = useQuery({
  queryKey: ['news', 'search', debouncedSearch],
  queryFn: () => fetchNews({ searchTerm: debouncedSearch }),
  enabled: debouncedSearch.length > 0,
});
```

**5. Chat-from-News Integration**

Users can click "Chat with AI" on any news item to create a new chat with the article content as context:

```javascript
// NewsModal component
const handleChatWithAI = async () => {
  const response = await fetch(API_ENDPOINTS.CHATS, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({
      message: `Tell me about: ${newsItem.title}`,
      newsItemId: newsItem.id, // Links chat to news article
    }),
  });

  const newChat = await response.json();
  navigate(`/chat/${newChat.id}`);
};
```

**Backend includes the full article content in the chat context automatically.**

---

### Message Management

#### Star/Favorite Messages

**Optimistic Update Pattern:**

```javascript
// src/hooks/useMessageActions.js
const toggleStar = async (messageId) => {
  // 1. Optimistically update UI
  dispatch(toggleMessageStar(messageId));

  try {
    // 2. Send request to backend
    const response = await fetch(API_ENDPOINTS.MESSAGE_STARRED(messageId), {
      method: 'PATCH',
      credentials: 'include',
    });

    if (!response.ok) throw new Error('Failed to toggle star');

    const result = await response.json();

    // 3. Update with server response
    dispatch(updateMessageStar({ messageId, isStarred: result.isStarred }));

  } catch (error) {
    // 4. Rollback on error
    dispatch(toggleMessageStar(messageId)); // Toggle back
    showError('Failed to update star status');
  }
};
```

**Benefits:**
- Instant UI feedback (no waiting for server)
- Automatic rollback on failure
- Server confirmation for data consistency

#### Report/Feedback System

```javascript
// ReportModal component
const submitReport = async (messageId, reason) => {
  const response = await fetch(API_ENDPOINTS.MESSAGE_REPORT(messageId), {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ reason }),
  });

  if (response.ok) {
    const result = await response.json();
    dispatch(markMessageReported({
      messageId,
      reportReason: result.reportReason,
      reportedAt: result.reportedAt,
    }));
  }
};
```

#### Copy to Clipboard

```javascript
// MessageList component
const copyToClipboard = (content) => {
  navigator.clipboard.writeText(content)
    .then(() => {
      // Visual feedback
      setShowCopiedMessage(true);
      setTimeout(() => setShowCopiedMessage(false), 2000);
    })
    .catch(err => console.error('Copy failed:', err));
};
```

---

### Authentication Flow

#### Email/Password Authentication

**1. Registration Flow:**

```
User fills signup form → Validation (email, password, full name)
    ↓
Submit to POST /auth
    ↓
Backend creates user with Identity
    ↓
Success response → Show success message
    ↓
User logs in with credentials
```

**Validation rules:**
- Email: Valid format, unique
- Password: Minimum 6 characters, must contain digits and lowercase
- Full Name: Required

**2. Login Flow:**

```
User fills login form → Email + Password
    ↓
Submit to POST /auth/login
    ↓
Backend validates credentials → Account lockout check (5 attempts)
    ↓
Backend generates JWT → Sets HttpOnly cookie
    ↓
Frontend receives success → Dispatch setUser(userInfo)
    ↓
AuthProvider detects authentication → Connects SignalR
    ↓
Redirect to /chat
```

**3. Session Persistence:**

```javascript
// AuthProvider.jsx - Check auth status on app load
useEffect(() => {
  const checkAuthStatus = async () => {
    try {
      const response = await fetch(API_ENDPOINTS.AUTH_STATUS, {
        credentials: 'include', // Send cookies
      });

      if (response.ok) {
        const data = await response.json();
        if (data.isAuthenticated) {
          dispatch(setUser({
            email: data.email,
            fullName: data.fullName,
          }));
        }
      }
    } catch (error) {
      console.error('Auth status check failed:', error);
    }
  };

  checkAuthStatus();
}, []);
```

**4. Logout Flow:**

```
User clicks Logout → Confirm action
    ↓
POST /auth/logout
    ↓
Backend clears auth cookie
    ↓
Frontend: dispatch(clearUser()) → Redux state cleared
    ↓
SignalR disconnection → chatHubService.stop()
    ↓
Redirect to /
```

#### Google OAuth2 Integration

**1. OAuth Flow:**

```
User clicks "Sign in with Google"
    ↓
Redirect to /auth/google-login (backend)
    ↓
Backend redirects to Google consent screen
    ↓
User grants permissions
    ↓
Google redirects to /auth/google-response (backend)
    ↓
Backend exchanges code for access token
    ↓
Backend retrieves user info from Google
    ↓
Backend creates/updates user in database
    ↓
Backend generates JWT → Sets HttpOnly cookie
    ↓
Backend redirects to /auth/callback (frontend)
    ↓
Frontend AuthCallback component checks auth status
    ↓
Dispatch setUser() → Navigate to /chat
```

**2. AuthCallback Component** (`src/pages/AuthCallback/AuthCallback.jsx`)

```javascript
const AuthCallback = () => {
  const dispatch = useDispatch();
  const navigate = useNavigate();

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Check if authentication succeeded
        const response = await fetch(API_ENDPOINTS.AUTH_STATUS, {
          credentials: 'include',
        });

        if (response.ok) {
          const data = await response.json();
          if (data.isAuthenticated) {
            dispatch(setUser({
              email: data.email,
              fullName: data.fullName,
            }));
            navigate('/chat');
          } else {
            navigate('/'); // Auth failed
          }
        }
      } catch (error) {
        console.error('OAuth callback error:', error);
        navigate('/');
      }
    };

    handleCallback();
  }, []);

  return <div>Completing authentication...</div>;
};
```

---

### Chat History & Sidebar

#### Sidebar Component

**Features:**
- Collapsible design with toggle button
- User section with profile and logout
- Chat list with time-relative formatting
- Chat actions (rename, delete)
- Skeleton loading states

**Time Formatting** (`src/hooks/useSidebar.js`):

```javascript
const formatChatTime = (lastMessageAt) => {
  const now = new Date();
  const messageDate = new Date(lastMessageAt);
  const diffMs = now - messageDate;
  const diffMins = Math.floor(diffMs / 60000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins} min ago`;

  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;

  const diffDays = Math.floor(diffHours / 24);
  if (diffDays === 1) return 'Yesterday';
  if (diffDays < 7) return `${diffDays} days ago`;

  return messageDate.toLocaleDateString();
};
```

#### Chat CRUD Operations

**1. Create Chat:**

```javascript
const createChat = async (initialMessage) => {
  const timezoneOffset = new Date().getTimezoneOffset();

  const response = await fetch(API_ENDPOINTS.CHATS, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({
      message: initialMessage,
      timezoneOffset, // For accurate server-side time handling
    }),
  });

  const newChat = await response.json();

  // Invalidate cache to refetch chat list
  queryClient.invalidateQueries(['chats']);

  return newChat;
};
```

**2. Rename Chat:**

```javascript
const renameChat = async (chatId, newTitle) => {
  if (newTitle.length < 1 || newTitle.length > 20) {
    throw new Error('Title must be 1-20 characters');
  }

  const response = await fetch(API_ENDPOINTS.CHAT_TITLE(chatId), {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ title: newTitle }),
  });

  if (response.ok) {
    queryClient.invalidateQueries(['chats']);
  }
};
```

**3. Delete Chat:**

```javascript
const deleteChat = async (chatId) => {
  if (!confirm('Are you sure you want to delete this chat?')) {
    return;
  }

  const response = await fetch(API_ENDPOINTS.CHAT_BY_ID(chatId), {
    method: 'DELETE',
    credentials: 'include',
  });

  if (response.ok) {
    queryClient.invalidateQueries(['chats']);
    navigate('/chat'); // Redirect if viewing deleted chat
  }
};
```

---

### Account Settings

Located in `src/components/AccountSettings/`, this feature provides comprehensive user profile management.

#### Email Update

```javascript
// EmailSection component
const updateEmail = async (newEmail) => {
  const response = await fetch(API_ENDPOINTS.UPDATE_EMAIL, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ email: newEmail }),
  });

  if (response.ok) {
    // Backend issues new JWT with updated email claim
    dispatch(updateUserEmail(newEmail));
    showSuccess('Email updated successfully');
  } else if (response.status === 429) {
    showError('Rate limit exceeded. Please try again later.');
  }
};
```

#### Password Change

```javascript
// PasswordSection component
const changePassword = async (currentPassword, newPassword) => {
  if (newPassword.length < 6 || !/\d/.test(newPassword) || !/[a-z]/.test(newPassword)) {
    showError('Password must be at least 6 characters with digits and lowercase letters');
    return;
  }

  const response = await fetch(API_ENDPOINTS.UPDATE_PASSWORD, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ currentPassword, newPassword }),
  });

  if (response.ok) {
    showSuccess('Password changed successfully');
    // Clear password fields
  } else {
    const error = await response.json();
    showError(error.message || 'Failed to change password');
  }
};
```

#### Newsletter Subscription

```javascript
// Newsletter component
const toggleNewsletter = async (isSubscribed) => {
  const response = await fetch(API_ENDPOINTS.UPDATE_NEWSLETTER, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ isSubscribed }),
  });

  if (response.ok) {
    const result = await response.json();
    setIsSubscribed(result.isSubscribedToNewsletter);
  }
};
```

#### Account Deletion

```javascript
// DeleteAccount component
const deleteAccount = async () => {
  const confirmed = confirm(
    'Are you sure you want to delete your account? This action cannot be undone.'
  );

  if (!confirmed) return;

  const response = await fetch(API_ENDPOINTS.DELETE_ACCOUNT, {
    method: 'DELETE',
    credentials: 'include',
  });

  if (response.ok) {
    // Clear local state
    dispatch(clearUser());

    // Disconnect SignalR
    await chatHubService.stop();

    // Redirect to home
    navigate('/');
  }
};
```

**Rate Limiting Handling:**

All account settings endpoints are rate-limited (10 requests per 10 minutes per IP). The frontend displays specific error messages for 429 responses:

```javascript
if (response.status === 429) {
  showError('Too many requests. Please wait a few minutes and try again.');
  setIsRateLimited(true);
}
```

## 📁 Project Structure

### Component Organization

**Feature-based structure:** Components are grouped by feature/domain rather than by type.

```
components/
├── Chat/                # All chat-related components
│   ├── ChatInput/
│   ├── MainChat/
│   └── MessageList/
├── Timeline/            # All timeline-related components
│   ├── Timeline.jsx
│   └── components/
└── AccountSettings/     # All profile-related components
    ├── EmailSection/
    ├── PasswordSection/
    └── DeleteAccount/
```

**Benefits:**
- Easy to find related components
- Clearer boundaries between features
- Better encapsulation
- Easier to refactor or remove features

### Custom Hooks Overview

| Hook | Purpose | Location |
|------|---------|----------|
| `useSignalRChat` | Core SignalR chat logic, message handling, chunk accumulation | `hooks/useSignalRChat.js` |
| `useSidebar` | Chat list management, formatting, CRUD operations | `hooks/useSidebar.js` |
| `useAutoScroll` | Auto-scroll to bottom on new messages | `hooks/useAutoScroll.js` |
| `useDatePagination` | Timeline date batch pagination (5-day increments) | `hooks/useDatePagination.js` |
| `useAccountSettings` | Profile form handling and submission | `hooks/useAccountSettings.js` |
| `useMessageActions` | Star, report, copy message operations | `hooks/useMessageActions.js` |

### State Management Structure

**Redux Slices:**

```
store/
├── index.js           # Store configuration
├── userAuth.js        # Authentication state
│   ├── State: { isAuthenticated, user: { email, fullName } }
│   ├── Actions: setUser, clearUser, updateUserEmail
├── chat.js            # Chat list state (deprecated - using TanStack Query now)
├── sidebarSlice.js    # Sidebar UI state
│   ├── State: { isOpen }
│   ├── Actions: toggleSidebar, setSidebarOpen
└── messagesSlice.js   # Messages state
    ├── State: { starredMessages[], reportedMessages[] }
    ├── Actions: toggleMessageStar, markMessageReported
```

**TanStack Query Keys:**

```javascript
// Chat queries
['chats']                           // All user chats
['chat', chatId]                    // Single chat details
['chat', chatId, 'messages']        // Chat message history

// Message queries
['messages', 'starred']             // All starred messages

// News queries
['news', dateTime, newsType]        // News by date and type
['news', 'search', searchTerm]      // News search results
```

## 🔄 State Management

### Redux vs TanStack Query Decision Tree

**Use Redux when:**
- State is UI-focused (sidebar open/closed, theme)
- State is synchronous and client-only
- State needs to be shared across many components
- State doesn't come from a server

**Use TanStack Query when:**
- Data comes from an API
- You need automatic caching
- You need loading/error states
- You need background refetching
- Data needs to be synchronized with server

### Example: Chat List

**Old approach (Redux only):**
```javascript
// ❌ Avoid - manual cache management
const fetchChats = async () => {
  dispatch(setLoading(true));
  try {
    const response = await fetch(API_ENDPOINTS.CHATS);
    const data = await response.json();
    dispatch(setChats(data));
  } catch (error) {
    dispatch(setError(error.message));
  } finally {
    dispatch(setLoading(false));
  }
};
```

**New approach (TanStack Query):**
```javascript
// ✅ Preferred - automatic cache management
const { data: chats, isLoading, error } = useQuery({
  queryKey: ['chats'],
  queryFn: async () => {
    const response = await fetch(API_ENDPOINTS.CHATS, {
      credentials: 'include',
    });
    if (!response.ok) throw new Error('Failed to fetch chats');
    return response.json();
  },
  staleTime: 5 * 60 * 1000, // 5 minutes
});
```

**Benefits of TanStack Query:**
- Automatic loading/error state management
- Built-in caching with configurable stale time
- Automatic refetching on window focus (optional)
- Request deduplication
- Optimistic updates support
- Less boilerplate code

### Invalidating Queries

When you perform mutations (create, update, delete), invalidate relevant queries to trigger refetch:

```javascript
import { useQueryClient } from '@tanstack/react-query';

const queryClient = useQueryClient();

// After creating a chat
await createChat(message);
queryClient.invalidateQueries(['chats']); // Refetch chat list

// After starring a message
await toggleStar(messageId);
queryClient.invalidateQueries(['messages', 'starred']); // Refetch starred messages

// After deleting a chat
await deleteChat(chatId);
queryClient.invalidateQueries(['chats']); // Refetch chat list
```

## 🔌 API Integration

### API Service Functions

All API calls are centralized in `src/api/` for consistency and maintainability.

#### Authentication (`src/api/auth.js`)

```javascript
export const register = async (email, password, fullName) => {
  const response = await fetch(API_ENDPOINTS.REGISTER, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, fullName }),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Registration failed');
  }

  return response.json();
};

export const login = async (email, password) => {
  const response = await fetch(API_ENDPOINTS.LOGIN, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include', // Important: Include cookies
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Login failed');
  }

  return response.json();
};

export const logout = async () => {
  const response = await fetch(API_ENDPOINTS.LOGOUT, {
    method: 'POST',
    credentials: 'include',
  });

  if (!response.ok) throw new Error('Logout failed');
  return response.json();
};

export const checkAuthStatus = async () => {
  const response = await fetch(API_ENDPOINTS.AUTH_STATUS, {
    credentials: 'include',
  });

  if (!response.ok) throw new Error('Auth status check failed');
  return response.json();
};
```

#### Chat Operations (`src/api/chat.js`)

```javascript
export const fetchChats = async () => {
  const timezoneOffset = new Date().getTimezoneOffset();

  const response = await fetch(`${API_ENDPOINTS.CHATS}?timezoneOffset=${timezoneOffset}`, {
    credentials: 'include',
  });

  if (!response.ok) throw new Error('Failed to fetch chats');
  return response.json();
};

export const createChat = async (message, newsItemId = null) => {
  const timezoneOffset = new Date().getTimezoneOffset();

  const response = await fetch(API_ENDPOINTS.CHATS, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ message, newsItemId, timezoneOffset }),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Failed to create chat');
  }

  return response.json();
};

export const deleteChat = async (chatId) => {
  const response = await fetch(API_ENDPOINTS.CHAT_BY_ID(chatId), {
    method: 'DELETE',
    credentials: 'include',
  });

  if (!response.ok) throw new Error('Failed to delete chat');
  return response.json();
};

export const updateChatTitle = async (chatId, title) => {
  const response = await fetch(API_ENDPOINTS.CHAT_TITLE(chatId), {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ title }),
  });

  if (!response.ok) throw new Error('Failed to update chat title');
  return response.json();
};
```

#### Messages (`src/api/messages.js`)

```javascript
export const fetchStarredMessages = async () => {
  const response = await fetch(API_ENDPOINTS.STARRED_MESSAGES, {
    credentials: 'include',
  });

  if (!response.ok) throw new Error('Failed to fetch starred messages');
  return response.json();
};

export const toggleMessageStar = async (messageId) => {
  const response = await fetch(API_ENDPOINTS.MESSAGE_STARRED(messageId), {
    method: 'PATCH',
    credentials: 'include',
  });

  if (!response.ok) throw new Error('Failed to toggle star');
  return response.json();
};

export const reportMessage = async (messageId, reason) => {
  const response = await fetch(API_ENDPOINTS.MESSAGE_REPORT(messageId), {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ reason }),
  });

  if (!response.ok) throw new Error('Failed to report message');
  return response.json();
};
```

### Error Handling Patterns

**1. Try-Catch with User Feedback:**

```javascript
const handleSubmit = async () => {
  try {
    const result = await apiFunction();
    showSuccess('Operation successful');
  } catch (error) {
    showError(error.message || 'Operation failed');
    console.error('Error:', error);
  }
};
```

**2. Response Status Handling:**

```javascript
const response = await fetch(url, options);

if (response.status === 401) {
  // Unauthorized - redirect to login
  dispatch(clearUser());
  navigate('/');
} else if (response.status === 429) {
  // Rate limited
  showError('Too many requests. Please wait and try again.');
} else if (response.status === 404) {
  // Not found
  showError('Resource not found');
} else if (!response.ok) {
  // Generic error
  const error = await response.json();
  throw new Error(error.message || 'Request failed');
}
```

**3. TanStack Query Error Handling:**

```javascript
const { data, error, isError } = useQuery({
  queryKey: ['resource'],
  queryFn: fetchResource,
  retry: 1, // Retry once on failure
  onError: (error) => {
    showError(error.message);
  },
});

if (isError) {
  return <ErrorMessage message={error.message} />;
}
```

## 🎣 Custom Hooks

### useSignalRChat

**Purpose:** Manages SignalR connection, message streaming, and chat state.

**Location:** `src/hooks/useSignalRChat.js`

**Key Features:**
- Connects to SignalR hub for specific chat
- Accumulates streaming message chunks
- Handles tool usage indicators
- Manages loading states
- Provides stop generation functionality

**Usage Example:**

```javascript
const ChatPage = ({ chatId }) => {
  const {
    messages,
    isLoading,
    sendMessage,
    stopGeneration,
    accumulatedChunks,
    currentTool,
  } = useSignalRChat(chatId);

  return (
    <div>
      <MessageList
        messages={messages}
        accumulatedChunks={accumulatedChunks}
      />
      {currentTool && <TypingIndicator tool={currentTool} />}
      <ChatInput
        onSend={sendMessage}
        onStop={stopGeneration}
        isLoading={isLoading}
      />
    </div>
  );
};
```

**Implementation Details:**

```javascript
const useSignalRChat = (chatId) => {
  const [messages, setMessages] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [accumulatedChunks, setAccumulatedChunks] = useState({});
  const [currentTool, setCurrentTool] = useState(null);

  useEffect(() => {
    // Join chat room
    chatHubService.joinChat(chatId);

    // Subscribe to events
    const handleReceiveMessage = (message) => {
      if (message.content.startsWith('[TOOL_START:')) {
        const toolName = message.content.match(/\[TOOL_START:(.*?)\]/)[1];
        setCurrentTool(toolName);
      } else if (message.content === '[TOOL_END]') {
        setCurrentTool(null);
      } else if (message.isChunk) {
        setAccumulatedChunks(prev => ({
          ...prev,
          [message.id]: (prev[message.id] || '') + message.content,
        }));
      } else {
        setMessages(prev => [...prev, message]);
        setAccumulatedChunks(prev => {
          const { [message.id]: removed, ...rest } = prev;
          return rest;
        });
      }
    };

    chatHubService.onReceiveMessage(handleReceiveMessage);

    // Cleanup
    return () => {
      chatHubService.offReceiveMessage(handleReceiveMessage);
    };
  }, [chatId]);

  const sendMessage = async (content) => {
    setIsLoading(true);
    try {
      await chatHubService.sendMessage(chatId, content);
    } catch (error) {
      console.error('Send error:', error);
      // Remove incomplete message
      setMessages(prev => prev.slice(0, -1));
    } finally {
      setIsLoading(false);
    }
  };

  const stopGeneration = async () => {
    await chatHubService.stopGeneration(chatId);
    setCurrentTool(null);
  };

  return {
    messages,
    isLoading,
    sendMessage,
    stopGeneration,
    accumulatedChunks,
    currentTool,
  };
};
```

---

### useSidebar

**Purpose:** Manages chat list fetching, formatting, and CRUD operations.

**Location:** `src/hooks/useSidebar.js`

**Key Features:**
- Fetches chat list with TanStack Query
- Formats timestamps (time-relative)
- Handles chat rename/delete
- Manages loading states

**Usage Example:**

```javascript
const Sidebar = () => {
  const {
    chats,
    isLoading,
    renameChat,
    deleteChat,
    formatChatTime,
  } = useSidebar();

  return (
    <div className={styles.sidebar}>
      {isLoading ? (
        <SkeletonLoader count={5} />
      ) : (
        chats.map(chat => (
          <ChatItem
            key={chat.id}
            chat={chat}
            time={formatChatTime(chat.lastMessageAt)}
            onRename={(newTitle) => renameChat(chat.id, newTitle)}
            onDelete={() => deleteChat(chat.id)}
          />
        ))
      )}
    </div>
  );
};
```

---

### useAutoScroll

**Purpose:** Automatically scrolls message container to bottom on new messages.

**Location:** `src/hooks/useAutoScroll.js`

**Key Features:**
- Detects when user is near bottom
- Only auto-scrolls if user hasn't manually scrolled up
- Smooth scroll behavior

**Usage Example:**

```javascript
const MessageList = ({ messages }) => {
  const { scrollRef, scrollToBottom, isNearBottom } = useAutoScroll(messages);

  return (
    <div ref={scrollRef} className={styles.messageContainer}>
      {messages.map(msg => <Message key={msg.id} {...msg} />)}
      {!isNearBottom && (
        <button onClick={scrollToBottom}>Scroll to bottom</button>
      )}
    </div>
  );
};
```

**Implementation:**

```javascript
const useAutoScroll = (dependencies) => {
  const scrollRef = useRef(null);
  const [isNearBottom, setIsNearBottom] = useState(true);

  useEffect(() => {
    const container = scrollRef.current;
    if (!container) return;

    // Check if user is near bottom (within 100px)
    const checkIfNearBottom = () => {
      const { scrollTop, scrollHeight, clientHeight } = container;
      const distanceFromBottom = scrollHeight - scrollTop - clientHeight;
      setIsNearBottom(distanceFromBottom < 100);
    };

    container.addEventListener('scroll', checkIfNearBottom);

    return () => container.removeEventListener('scroll', checkIfNearBottom);
  }, []);

  useEffect(() => {
    if (isNearBottom) {
      scrollToBottom();
    }
  }, [dependencies, isNearBottom]);

  const scrollToBottom = () => {
    scrollRef.current?.scrollTo({
      top: scrollRef.current.scrollHeight,
      behavior: 'smooth',
    });
  };

  return { scrollRef, scrollToBottom, isNearBottom };
};
```

---

### useDatePagination

**Purpose:** Manages date-based pagination for the timeline (5-day batches).

**Location:** `src/hooks/useDatePagination.js`

**Key Features:**
- Loads news in 5-day increments
- Tracks current date position
- Provides hasMore flag for infinite scroll

**Usage Example:**

```javascript
const Timeline = () => {
  const { dateRanges, loadNextBatch, hasMore } = useDatePagination();

  return (
    <InfiniteScroll
      dataLength={dateRanges.length}
      next={loadNextBatch}
      hasMore={hasMore}
      loader={<Spinner />}
    >
      {dateRanges.map(range => (
        <NewsSection key={range.startDate} {...range} />
      ))}
    </InfiniteScroll>
  );
};
```

## 🎨 Component Patterns

### CSS Modules Convention

**File naming:** `ComponentName.module.css`

**Usage:**

```javascript
// Component.jsx
import styles from './Component.module.css';

const Component = () => {
  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Hello</h1>
      <p className={styles.textContent}>Content</p>
    </div>
  );
};
```

```css
/* Component.module.css */
.container {
  padding: 1rem;
}

.title {
  font-size: 2rem;
}

.textContent {  /* Automatically camelCased in JS */
  color: var(--text-color);
}
```

**Benefits:**
- Scoped styles (no naming conflicts)
- Automatic camelCase conversion
- Better tree-shaking
- Co-located with components

### Conditional Rendering Pattern

```javascript
// ✅ Preferred - early returns for loading/error states
const Component = () => {
  const { data, isLoading, error } = useQuery(...);

  if (isLoading) return <Skeleton />;
  if (error) return <ErrorMessage error={error} />;
  if (!data) return <EmptyState />;

  return <DataView data={data} />;
};
```

### Component Composition Pattern

```javascript
// ✅ Preferred - small, focused components
const ChatPage = () => {
  return (
    <div className={styles.chatPage}>
      <Sidebar />
      <MainChat />
    </div>
  );
};

const MainChat = () => {
  return (
    <div className={styles.mainChat}>
      <Header />
      <MessageList />
      <ChatInput />
    </div>
  );
};
```

### Props Interface Pattern

```javascript
// ✅ Preferred - destructure props with defaults
const Message = ({
  content,
  type = 'user',
  timestamp,
  isStarred = false,
  onStar,
  onReport,
}) => {
  return (
    <div className={styles.message}>
      <div className={styles.content}>{content}</div>
      <div className={styles.actions}>
        <button onClick={onStar}>
          {isStarred ? '⭐' : '☆'}
        </button>
        <button onClick={onReport}>Report</button>
      </div>
    </div>
  );
};
```

## 💻 Development

### Project Conventions

**Naming:**
- Components: PascalCase (`ChatInput.jsx`, `MessageList.jsx`)
- Hooks: camelCase with `use` prefix (`useSignalRChat.js`, `useSidebar.js`)
- Utilities: camelCase (`validation.js`, `auth.js`)
- CSS Modules: PascalCase with `.module.css` suffix (`Component.module.css`)
- Constants: UPPER_SNAKE_CASE (`API_BASE_URL`, `CHAT_HUB_URL`)

**File Organization:**
- Group by feature/domain
- Keep related files together
- Use index.js for barrel exports

**Code Style:**
- Use functional components with hooks
- Prefer const over let
- Use arrow functions
- Destructure props
- Use optional chaining (`?.`)
- Use nullish coalescing (`??`)

### Adding New Features

**Example: Adding a "Favorite Chats" feature**

**1. Create API service function:**

```javascript
// src/api/chat.js
export const toggleChatFavorite = async (chatId) => {
  const response = await fetch(`${API_ENDPOINTS.CHAT_BY_ID(chatId)}/favorite`, {
    method: 'PATCH',
    credentials: 'include',
  });

  if (!response.ok) throw new Error('Failed to toggle favorite');
  return response.json();
};
```

**2. Update Redux slice (if needed for local state):**

```javascript
// src/store/chat.js
const chatSlice = createSlice({
  name: 'chat',
  initialState: {
    favoriteChats: [],
  },
  reducers: {
    toggleFavorite: (state, action) => {
      const chatId = action.payload;
      const index = state.favoriteChats.indexOf(chatId);
      if (index > -1) {
        state.favoriteChats.splice(index, 1);
      } else {
        state.favoriteChats.push(chatId);
      }
    },
  },
});
```

**3. Create custom hook:**

```javascript
// src/hooks/useFavoriteChats.js
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toggleChatFavorite } from '../api/chat';

export const useFavoriteChats = () => {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: toggleChatFavorite,
    onSuccess: () => {
      queryClient.invalidateQueries(['chats']);
    },
  });

  return {
    toggleFavorite: mutation.mutate,
    isLoading: mutation.isLoading,
  };
};
```

**4. Update component:**

```javascript
// src/components/Sidebar/ChatItem.jsx
const ChatItem = ({ chat }) => {
  const { toggleFavorite, isLoading } = useFavoriteChats();

  return (
    <div className={styles.chatItem}>
      <h3>{chat.title}</h3>
      <button
        onClick={() => toggleFavorite(chat.id)}
        disabled={isLoading}
      >
        {chat.isFavorite ? '⭐' : '☆'}
      </button>
    </div>
  );
};
```

**5. Add tests (optional but recommended):**

```javascript
// src/hooks/__tests__/useFavoriteChats.test.js
import { renderHook, waitFor } from '@testing-library/react';
import { useFavoriteChats } from '../useFavoriteChats';

describe('useFavoriteChats', () => {
  it('should toggle favorite status', async () => {
    const { result } = renderHook(() => useFavoriteChats());

    result.current.toggleFavorite('chat-id-123');

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });
  });
});
```

### Development Workflow

**1. Start backend server:**
```bash
cd backend
dotnet run
```

**2. Start frontend dev server:**
```bash
cd frontend
npm run dev
```

**3. Make changes:**
- Edit files in `src/`
- Vite HMR will auto-reload changes
- Check browser console for errors

**4. Test changes:**
- Manual testing in browser
- Check network tab for API calls
- Verify SignalR connection in console

**5. Build for production:**
```bash
npm run build
npm run preview  # Preview production build
```

### Debugging Tips

**SignalR Connection Issues:**

```javascript
// Enable detailed logging
import { LogLevel } from '@microsoft/signalr';

this.connection = new HubConnectionBuilder()
  .withUrl(CHAT_HUB_URL, { withCredentials: true })
  .configureLogging(LogLevel.Debug)  // Add this line
  .withAutomaticReconnect()
  .build();
```

**Redux DevTools:**

Install Redux DevTools browser extension to inspect state changes:
- Time-travel debugging
- Action history
- State diff visualization

**React DevTools:**

Install React DevTools to inspect component tree:
- Component hierarchy
- Props and state
- Performance profiling

**Network Debugging:**

Browser DevTools → Network tab:
- Check API request/response
- Verify cookies are sent
- Check SignalR WebSocket connection
- Monitor request timing

## 🐛 Troubleshooting

### Common Issues

#### 1. SignalR Connection Fails

**Symptoms:**
- "Failed to connect to SignalR" error
- No real-time messages
- Connection keeps reconnecting

**Solutions:**

**Check backend is running:**
```bash
curl http://localhost:5170/auth/status
```

**Verify CORS configuration in backend:**
```csharp
// Backend Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for SignalR
    });
});
```

**Check browser console for detailed errors:**
```
Failed to start the connection: Error: WebSocket failed to connect.
```

**Enable SignalR debug logging:**
```javascript
// src/api/chatHub.js
import { LogLevel } from '@microsoft/signalr';

.configureLogging(LogLevel.Debug)
```

---

#### 2. Authentication Issues

**Symptoms:**
- "Unauthorized" errors
- Redirected to login after refresh
- Session not persisting

**Solutions:**

**Verify cookies are being set:**
- Browser DevTools → Application → Cookies
- Look for authentication cookie
- Check `HttpOnly`, `Secure`, `SameSite` attributes

**Check `credentials: 'include'` in all API calls:**
```javascript
// ✅ Correct
fetch(url, { credentials: 'include' })

// ❌ Wrong
fetch(url) // Missing credentials
```

**Clear browser cookies and re-login:**
```javascript
// Manually clear cookies in console
document.cookie.split(";").forEach(c => {
  document.cookie = c.replace(/^ +/, "").replace(/=.*/, "=;expires=" + new Date().toUTCString() + ";path=/");
});
```

**Verify JWT token hasn't expired:**
- Default expiration: 10,000 minutes (~7 days)
- Check backend JwtSettings configuration

---

#### 3. CORS Errors

**Symptoms:**
- "Access to fetch at ... has been blocked by CORS policy"
- Network requests fail with CORS error

**Solutions:**

**Update backend CORS policy:**
```csharp
policy.WithOrigins("http://localhost:5173") // Match frontend URL exactly
      .AllowCredentials(); // Required for cookies
```

**Check API_BASE_URL matches backend:**
```javascript
// src/api/config.js
export const API_BASE_URL = "http://localhost:5170"; // Must match backend
```

**Ensure backend is running on expected port:**
```bash
# Backend should show:
Now listening on: http://localhost:5170
```

---

#### 4. Build Errors

**Symptoms:**
- `npm run build` fails
- Vite compilation errors
- Module not found errors

**Solutions:**

**Clear node_modules and reinstall:**
```bash
rm -rf node_modules package-lock.json
npm install
```

**Check for missing dependencies:**
```bash
npm install
```

**Verify Node.js version:**
```bash
node --version  # Should be v18+
```

**Check for syntax errors:**
```bash
npm run lint
```

---

#### 5. Rate Limiting Errors

**Symptoms:**
- "Too many requests" (429) errors
- Account settings updates fail
- Frequent error popups

**Solutions:**

**Wait for rate limit window to reset:**
- Auth endpoints: 5 minutes
- Profile endpoints: 10 minutes
- Chat endpoints: 1 minute

**Check for request loops in code:**
- Ensure useEffect has proper dependencies
- Verify no infinite re-render loops
- Check TanStack Query configuration

**Implement exponential backoff:**
```javascript
const retryWithBackoff = async (fn, retries = 3) => {
  for (let i = 0; i < retries; i++) {
    try {
      return await fn();
    } catch (error) {
      if (error.status === 429 && i < retries - 1) {
        const delay = Math.pow(2, i) * 1000; // 1s, 2s, 4s
        await new Promise(resolve => setTimeout(resolve, delay));
      } else {
        throw error;
      }
    }
  }
};
```

---

#### 6. State Not Updating

**Symptoms:**
- UI doesn't reflect changes
- Data appears stale
- Cache not invalidating

**Solutions:**

**Check TanStack Query cache invalidation:**
```javascript
// After mutation
queryClient.invalidateQueries(['chats']);
```

**Verify Redux dispatch:**
```javascript
// Check action is dispatched
dispatch(setUser(userData));
```

**Use React DevTools to inspect state:**
- Components tab → Select component → Check props/state

**Force refetch:**
```javascript
const { refetch } = useQuery(...);
refetch();
```

---

#### 7. Message Streaming Issues

**Symptoms:**
- Messages appear all at once instead of streaming
- Chunks not displaying
- Tool indicators not showing

**Solutions:**

**Check SignalR event handlers:**
```javascript
// Ensure ReceiveMessage handler is registered
chatHubService.onReceiveMessage(handleReceiveMessage);
```

**Verify chunk accumulation logic:**
```javascript
// Check accumulatedChunks state in useSignalRChat
console.log('Accumulated chunks:', accumulatedChunks);
```

**Check backend streaming configuration:**
- Ensure backend is sending chunks with `isChunk: true`
- Verify OpenAI streaming is enabled

**Monitor SignalR events in console:**
```javascript
chatHubService.connection.on('ReceiveMessage', (msg) => {
  console.log('Received:', msg);
});
```

---

#### 8. CSS Styling Issues

**Symptoms:**
- Styles not applying
- Class names not matching
- CSS conflicts

**Solutions:**

**Verify CSS module import:**
```javascript
// ✅ Correct
import styles from './Component.module.css';

// ❌ Wrong
import './Component.css'; // Not a CSS module
```

**Check camelCase class names:**
```javascript
// CSS: .text-content
// JS: styles.textContent (camelCased automatically)
```

**Clear Vite cache:**
```bash
rm -rf node_modules/.vite
npm run dev
```

**Check for CSS specificity issues:**
```css
/* Use more specific selectors if needed */
.container .button {
  /* ... */
}
```

---

### Performance Profiling

**React DevTools Profiler:**

1. Install React DevTools browser extension
2. Open DevTools → Profiler tab
3. Click record
4. Interact with app
5. Stop recording
6. Analyze component render times

**Vite Build Analysis:**

```bash
npm run build -- --mode analyze
```

**Bundle size visualization:**

```bash
npm install -D rollup-plugin-visualizer
```

Add to `vite.config.js`:
```javascript
import { visualizer } from 'rollup-plugin-visualizer';

export default defineConfig({
  plugins: [
    react(),
    visualizer({ open: true }),
  ],
});
```

**Network Performance:**

- Browser DevTools → Network tab
- Enable "Disable cache"
- Reload page
- Sort by Size or Time
- Identify large resources

## 🚀 Deployment

### Production Build

**1. Build the application:**

```bash
npm run build
```

This creates an optimized production build in the `dist/` folder.

**Build output:**
```
dist/
├── index.html
├── assets/
│   ├── index-[hash].js      # Main application bundle
│   ├── vendor-[hash].js     # Third-party libraries
│   ├── redux-[hash].js      # Redux bundle
│   ├── query-[hash].js      # TanStack Query bundle
│   ├── signalr-[hash].js    # SignalR bundle
│   ├── markdown-[hash].js   # Markdown rendering bundle
│   └── index-[hash].css     # Styles
└── ... (other assets)
```

**2. Preview production build locally:**

```bash
npm run preview
```

**3. Test production build:**

- Verify all features work
- Check SignalR connection
- Test authentication flow
- Verify API calls work with production backend

### Environment Configuration

**Production environment variables:**

Create `.env.production`:

```env
VITE_API_BASE_URL=https://api.yourdomain.com
VITE_ENVIRONMENT=production
```

**Update config.js:**

```javascript
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5170";
```

### Deployment Platforms

#### Vercel (Recommended)

**1. Install Vercel CLI:**
```bash
npm install -g vercel
```

**2. Deploy:**
```bash
cd frontend
vercel
```

**3. Configure environment variables in Vercel dashboard:**
- `VITE_API_BASE_URL` → Your backend URL

**4. Set build command:**
- Build Command: `npm run build`
- Output Directory: `dist`

**vercel.json configuration:**

```json
{
  "rewrites": [
    { "source": "/(.*)", "destination": "/" }
  ]
}
```

#### Netlify

**1. Install Netlify CLI:**
```bash
npm install -g netlify-cli
```

**2. Deploy:**
```bash
netlify deploy --prod
```

**3. Configure:**
- Build Command: `npm run build`
- Publish Directory: `dist`

**netlify.toml configuration:**

```toml
[build]
  command = "npm run build"
  publish = "dist"

[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
```

#### Docker

**Dockerfile:**

```dockerfile
# Build stage
FROM node:18-alpine AS build

WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Production stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**nginx.conf:**

```nginx
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

**Build and run:**
```bash
docker build -t ai-track-frontend .
docker run -p 80:80 ai-track-frontend
```

### Post-Deployment Checklist

- [ ] Verify frontend loads correctly
- [ ] Test authentication (login/signup)
- [ ] Verify SignalR connection works
- [ ] Test chat functionality
- [ ] Verify timeline loads news
- [ ] Test all API endpoints
- [ ] Check browser console for errors
- [ ] Verify CORS configuration
- [ ] Test on multiple browsers
- [ ] Check mobile responsiveness
- [ ] Verify environment variables are set
- [ ] Test error handling
- [ ] Check SSL certificate (HTTPS)

## 📜 Scripts

```json
{
  "scripts": {
    "dev": "vite",                    // Start development server with HMR
    "build": "vite build",            // Build for production (optimized)
    "preview": "vite preview",        // Preview production build locally
    "lint": "eslint .",               // Run ESLint for code quality
    "lint:fix": "eslint . --fix"      // Auto-fix linting issues
  }
}
```

**Development:**
```bash
npm run dev
```

**Production build:**
```bash
npm run build
npm run preview
```

**Code quality:**
```bash
npm run lint
npm run lint:fix
```

## 📄 License

This project is licensed under the MIT License. See the LICENSE file for details.

## 🙏 Acknowledgments

This frontend leverages powerful open-source libraries and tools:

- **[React](https://react.dev/)** - Modern UI library with concurrent features
- **[Vite](https://vitejs.dev/)** - Lightning-fast build tool with HMR
- **[Redux Toolkit](https://redux-toolkit.js.org/)** - Simplified Redux state management
- **[TanStack Query](https://tanstack.com/query/latest)** - Powerful server state management
- **[Microsoft SignalR](https://www.npmjs.com/package/@microsoft/signalr)** - Real-time WebSocket communication
- **[React Router](https://reactrouter.com/)** - Declarative routing for React
- **[react-markdown](https://github.com/remarkjs/react-markdown)** - Markdown rendering component
- **[react-syntax-highlighter](https://github.com/react-syntax-highlighter/react-syntax-highlighter)** - Syntax highlighting for code blocks
- **[remark-gfm](https://github.com/remarkjs/remark-gfm)** - GitHub Flavored Markdown support
- **[react-infinite-scroll-component](https://github.com/ankeetmaini/react-infinite-scroll-component)** - Infinite scroll implementation

---

**Built with modern React practices and optimized for performance.**
