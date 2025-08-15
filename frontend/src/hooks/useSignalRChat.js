import { useState, useEffect, useCallback } from "react";
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

  // Handle incoming messages from SignalR
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
    setIsSendingMessage(false);
  }, []);

  const handleChunkMessageReceived = useCallback((chunkMessage) => {
    // Streaming chunk from backend, accumulate chunk content
    setMessages((prev) => {
      // Find previous chunk message (if any)
      const lastChunk = prev.find((msg) => msg.isChunkMessage);
      const filtered = prev.filter((msg) => !msg.isChunkMessage);
      const newContent = lastChunk
        ? lastChunk.content + chunkMessage.content
        : chunkMessage.content;
      return [
        ...filtered,
        {
          content: newContent,
          type: "assistant",
          isChunkMessage: true,
        },
      ];
    });
  }, []);

  // Handle chat joined event
  const handleChatJoined = useCallback((joinedChatId, title, chatMessages) => {
    // Transform messages to component format
    const transformedMessages = chatMessages.map((msg) => ({
      content: msg.content,
      type: msg.type === 0 ? "user" : "assistant",
      createdAt: msg.createdAt,
      id: msg.id,
    }));

    setMessages(transformedMessages);
    setIsLoading(false);

    // Scroll to bottom when chat loads
    setTimeout(() => {}, 100);
  }, []);

  // Handle SignalR errors
  const handleSignalRError = useCallback((error) => {
    console.error("SignalR error in component:", error);
    setError("Connection error occurred");
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

        // Start SignalR connection first
        await chatHubService.startConnection();

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
          setMessages([
            {
              content: location.state.initialMessage,
              type: "user",
              createdAt: new Date().toISOString(),
              id: `temp-${Date.now()}`, // Temporary ID until real message comes from SignalR
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
    };
  }, [
    chatId,
    isUserLoggedIn,
    navigate,
    handleFullMessageReceived,
    handleChunkMessageReceived,
    handleChatJoined,
    handleSignalRError,
    location.state,
  ]);

  const sendMessage = async (messageText) => {
    if (!isUserLoggedIn || !chatId || isSendingMessage) {
      return;
    }

    setIsSendingMessage(true);

    try {
      // Add user message to UI immediately - optimistic update
      const userMessage = {
        content: messageText,
        type: "user",
        createdAt: new Date().toISOString(),
        id: Date.now(),
      };
      setMessages((prev) => [...prev, userMessage]);

      // Send message via SignalR
      await chatHubService.sendMessage(chatId, messageText);

      // Trigger sidebar refresh to update chat order and last message time
      dispatch(chatSliceActions.triggerChatRefresh());
    } catch (error) {
      console.error("Error sending message:", error);
      setIsSendingMessage(false);
    }
  };

  return {
    messages,
    isLoading,
    error,
    isSendingMessage,
    sendMessage,
  };
}
