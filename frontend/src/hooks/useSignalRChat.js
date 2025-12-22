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

  const handleFullMessageReceived = useCallback((message) => {
    setMessages((prev) => {
      const filtered = prev.filter((msg) => !msg.isChunkMessage);
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

  const handleChunkMessageReceived = useCallback((chunkMessage) => {
    const toolStartMatch = chunkMessage.content.match(
      /\[TOOL_START:([^\]]+)\]/
    );

    if (toolStartMatch) {
      const toolName = toolStartMatch[1];
      setCurrentTool(toolName);
      return;
    }

    if (chunkMessage.content && !chunkMessage.content.startsWith("[TOOL_")) {
      setCurrentTool(null);
    }

    setMessages((prev) => {
      const lastIndex = prev.length - 1;
      const lastMessage = prev[lastIndex];

      if (lastMessage && lastMessage.isChunkMessage) {
        const updated = [...prev];
        updated[lastIndex] = {
          ...lastMessage,
          content: lastMessage.content + chunkMessage.content,
        };
        return updated;
      } else {
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

  const handleSignalRError = useCallback((error) => {
    setMessages((prev) => {
      if (prev.length > 0) {
        const messagesWithoutLast = prev.slice(0, -1);
        return messagesWithoutLast;
      }
      return prev;
    });

    if (errorTimeoutRef.current) {
      clearTimeout(errorTimeoutRef.current);
    }

    setErrorMessage(error);
    errorTimeoutRef.current = setTimeout(() => {
      setErrorMessage("");
      errorTimeoutRef.current = null;
    }, 5000);

    setIsLoading(false);
    setIsSendingMessage(false);
  }, []);

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

        if (chatHubService.getConnectionState() === "Disconnected") {
          await chatHubService.startConnection();
        }

        unsubscribeFullMessage = chatHubService.onFullMessageReceived(
          handleFullMessageReceived
        );
        unsubscribeChunkMessage = chatHubService.onChunkMessageReceived(
          handleChunkMessageReceived
        );
        unsubscribeChatJoined = chatHubService.onChatJoined(handleChatJoined);
        unsubscribeError = chatHubService.onError(handleSignalRError);

        await chatHubService.joinChat(chatId);

        if (location.state?.initialMessage) {
          messageIdCounterRef.current += 1;
          setMessages([
            {
              content: location.state.initialMessage,
              type: "user",
              createdAt: new Date().toISOString(),
              id: `temp-${Date.now()}-${messageIdCounterRef.current}`,
            },
          ]);
          setIsSendingMessage(true);
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

    return () => {
      if (unsubscribeFullMessage) unsubscribeFullMessage();
      if (unsubscribeChunkMessage) unsubscribeChunkMessage();
      if (unsubscribeChatJoined) unsubscribeChatJoined();
      if (unsubscribeError) unsubscribeError();

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
      messageIdCounterRef.current += 1;
      const userMessage = {
        content: messageText,
        type: "user",
        createdAt: new Date().toISOString(),
        id: `user-${Date.now()}-${messageIdCounterRef.current}`,
      };
      setMessages((prev) => [...prev, userMessage]);

      await chatHubService.sendMessage(chatId, messageText);

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
    } catch (err) {
      console.error("Error cancelling generation:", err);
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
