import { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useDispatch } from "react-redux";
import { chatSliceActions } from "../store/chat";
import chatHubService from "../api/chatHub";

export function useSignalRChat(chatId, isUserLoggedIn) {
  const navigate = useNavigate();
  const location = useLocation();
  const dispatch = useDispatch();
  const [messages, setMessages] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSendingMessage, setIsSendingMessage] = useState(false);
  const [error, setError] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [currentTool, setCurrentTool] = useState(null);
  const errorTimeoutRef = useRef(null);
  const messageIdCounterRef = useRef(0);
  const isCancellingRef = useRef(false);

  // useCallback for functions that are passed as event handler to signalr. I do not want resubscribe every render

  // Handle full message from SignalR
  const handleFullMessageReceived = useCallback((message) => {
    // Final message from backend, replace any temp/streaming message
    setMessages((prev) => {
      // Remove any chunk messages
      const filtered = prev.filter((msg) => !msg.isChunkMessage);
      // Add the full message
      return [
        ...filtered,
        {
          content: message.content,
          type: message.type === "User" ? "user" : "assistant",
          createdAt: message.createdAt,
          id: message.id,
          isChunkMessage: false,
        },
      ];
    });
    setCurrentTool(null);
    setIsSendingMessage(false);
  }, []);

  // Handle chunk message
  const handleChunkMessageReceived = useCallback((chunkMessage) => {
    // Check for tool execution markers
    const toolStartMatch = chunkMessage.content.match(
      /\[TOOL_START:([^\]]+)\]/
    );

    if (toolStartMatch) {
      // Extract tool name and update current tool state
      const toolName = toolStartMatch[1];
      setCurrentTool(toolName);
      // Don't add this marker to messages, just update the tool state
      return;
    }

    // If we receive actual content, clear the tool state (tool execution finished)
    if (chunkMessage.content && !chunkMessage.content.startsWith("[TOOL_")) {
      setCurrentTool(null);
    }

    setMessages((prev) => {
      const lastIndex = prev.length - 1;
      const lastMessage = prev[lastIndex];

      if (lastMessage && lastMessage.isChunkMessage) {
        // Update existing chunk message in place
        const updated = [...prev];
        updated[lastIndex] = {
          ...lastMessage,
          content: lastMessage.content + chunkMessage.content,
        };
        return updated;
      } else {
        // First chunk - add new chunk message
        return [
          ...prev,
          {
            content: chunkMessage.content,
            type: "assistant",
            isChunkMessage: true,
          },
        ];
      }
    });
  }, []);

  // Handle chat joined event
  const handleChatJoined = useCallback((joinedChatId, title, chatMessages) => {
    const transformedMessages = chatMessages.map((msg) => ({
      content: msg.content,
      type: msg.type === 0 ? "user" : "assistant",
      createdAt: msg.createdAt,
      id: msg.id,
      isStarred: msg.isStarred,
    }));

    setMessages(transformedMessages);
    setIsLoading(false);
  }, []);

  // Handle SignalR errors
  const handleSignalRError = useCallback((error) => {
    // Remove the last message (user's message that caused the error)
    setMessages((prev) => {
      if (prev.length > 0) {
        const messagesWithoutLast = prev.slice(0, -1); // Remove last message
        return messagesWithoutLast;
      }
      return prev;
    });

    // Clear any existing timeout
    if (errorTimeoutRef.current) {
      clearTimeout(errorTimeoutRef.current);
    }

    // Show error message in popup for 5 seconds
    setErrorMessage(error);
    errorTimeoutRef.current = setTimeout(() => {
      setErrorMessage("");
      errorTimeoutRef.current = null;
    }, 5000);

    setIsLoading(false);
    setIsSendingMessage(false);
  }, []);

  // Initialize chat and SignalR connection
  useEffect(() => {
    if (!isUserLoggedIn || !chatId) {
      navigate("/chat/new");
      return;
    }

    let unsubscribeFullMessage,
      unsubscribeChunkMessage,
      unsubscribeChatJoined,
      unsubscribeError;

    const initializeChat = async () => {
      try {
        setIsLoading(true);
        setError(null);

        // Start SignalR connection (only if not already connected)
        if (chatHubService.getConnectionState() === "Disconnected") {
          await chatHubService.startConnection();
        }

        // Set up SignalR event handlers
        unsubscribeFullMessage = chatHubService.onFullMessageReceived(
          handleFullMessageReceived
        );
        unsubscribeChunkMessage = chatHubService.onChunkMessageReceived(
          handleChunkMessageReceived
        );
        unsubscribeChatJoined = chatHubService.onChatJoined(handleChatJoined);
        unsubscribeError = chatHubService.onError(handleSignalRError);

        // Join the chat via SignalR
        await chatHubService.joinChat(chatId);

        // Check if there's an initial message from navigation state
        if (location.state?.initialMessage) {
          // Add the initial message to state immediately
          messageIdCounterRef.current += 1;
          setMessages([
            {
              content: location.state.initialMessage,
              type: "user",
              createdAt: new Date().toISOString(),
              id: `temp-${Date.now()}-${messageIdCounterRef.current}`, // More unique ID
            },
          ]);
          setIsSendingMessage(true); // Show that we're waiting for AI response
        }
      } catch (error) {
        console.error("Error initializing chat:", error);
        if (error.message === "Chat not found") {
          setError("Chat not found");
        } else {
          setError("Failed to load chat");
        }
        setIsLoading(false);
      }
    };

    initializeChat();

    // Cleanup function - runs on unmount or when dependencies change
    return () => {
      if (unsubscribeFullMessage) unsubscribeFullMessage();
      if (unsubscribeChunkMessage) unsubscribeChunkMessage();
      if (unsubscribeChatJoined) unsubscribeChatJoined();
      if (unsubscribeError) unsubscribeError();

      // Clear error timeout on cleanup
      if (errorTimeoutRef.current) {
        clearTimeout(errorTimeoutRef.current);
        errorTimeoutRef.current = null;
      }
    };
  }, [
    chatId,
    isUserLoggedIn,
    navigate,
    handleFullMessageReceived,
    handleChunkMessageReceived,
    handleChatJoined,
    handleSignalRError,
    location.state?.initialMessage,
  ]);

  const sendMessage = async (messageText) => {
    if (!isUserLoggedIn || !chatId || isSendingMessage) {
      return;
    }

    setIsSendingMessage(true);

    try {
      // Add user message to UI immediately - optimistic update
      messageIdCounterRef.current += 1;
      const userMessage = {
        content: messageText,
        type: "user",
        createdAt: new Date().toISOString(),
        id: `user-${Date.now()}-${messageIdCounterRef.current}`, // More unique ID
      };
      setMessages((prev) => [...prev, userMessage]);

      // Send message via SignalR
      await chatHubService.sendMessage(chatId, messageText);

      // Update chat order in Redux (move to top with updated time)
      dispatch(chatSliceActions.updateChatOrder(chatId));
    } catch (error) {
      console.error("Error sending message:", error);
      setIsSendingMessage(false);
    }
  };

  const cancelGeneration = async () => {
    if (!isUserLoggedIn || !chatId || !isSendingMessage) return;
    try {
      isCancellingRef.current = true;
      await chatHubService.stopGeneration(chatId);
      // Keep isSendingMessage true until backend emits final partial message;
      // it will be set false in handleFullMessageReceived or error handler.
    } catch (err) {
      console.error("Error cancelling generation:", err);
      // As a fallback, stop waiting state to unblock UI.
      setIsSendingMessage(false);
    } finally {
      isCancellingRef.current = false;
    }
  };

  return {
    messages,
    isLoading,
    error,
    isSendingMessage,
    sendMessage,
    cancelGeneration,
    errorMessage,
    currentTool,
  };
}
