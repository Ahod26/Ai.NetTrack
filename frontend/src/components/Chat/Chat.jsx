import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { getChatById } from "../../api/chat";
import chatHubService from "../../api/chatHub";
import ChatInput from "../ChatInput/ChatInput";
import styles from "./Chat.module.css";

export default function Chat() {
  const { chatId } = useParams();
  const navigate = useNavigate();
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);

  const [messages, setMessages] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSendingMessage, setIsSendingMessage] = useState(false);
  const [error, setError] = useState(null);

  // Handle incoming messages from SignalR
  const handleMessageReceived = useCallback((message) => {
    console.log("Received message in component:", message);
    setMessages((prev) => [
      ...prev,
      {
        content: message.Content || message.content, // Handle both cases
        type: message.Type === "User" ? "user" : "assistant", // Convert SignalR format
        createdAt:
          message.CreatedAt || message.createdAt || new Date().toISOString(),
        id: message.Id || message.id || Date.now(),
      },
    ]);
    setIsSendingMessage(false);
  }, []);

  // Handle chat joined event
  const handleChatJoined = useCallback((joinedChatId, title, chatMessages) => {
    console.log("Chat joined event:", { joinedChatId, title, chatMessages });

    // Transform messages to component format
    const transformedMessages = chatMessages.map((msg) => ({
      content: msg.content, // lowercase from backend
      type: msg.type === 0 ? "user" : "assistant", // Convert 0 -> "user", 1 -> "assistant"
      createdAt: msg.createdAt, // lowercase from backend
      id: msg.id, // lowercase from backend
    }));

    setMessages(transformedMessages);
    setIsLoading(false);
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

    const initializeChat = async () => {
      try {
        setIsLoading(true);
        setError(null);

        // First, verify chat exists via REST API
        const chat = await getChatById(chatId);
        console.log("Retrieved chat:", chat);

        // Set up SignalR event handlers
        const unsubscribeMessage = chatHubService.onMessageReceived(
          handleMessageReceived
        );
        const unsubscribeChatJoined =
          chatHubService.onChatJoined(handleChatJoined);
        const unsubscribeError = chatHubService.onError(handleSignalRError);

        // Join the chat via SignalR
        await chatHubService.joinChat(chatId);

        // Cleanup function
        return () => {
          unsubscribeMessage();
          unsubscribeChatJoined();
          unsubscribeError();
        };
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
  }, [
    chatId,
    isUserLoggedIn,
    navigate,
    handleMessageReceived,
    handleChatJoined,
    handleSignalRError,
  ]);

  const handleSendMessage = async (messageText) => {
    if (!isUserLoggedIn || !chatId || isSendingMessage) {
      return;
    }

    setIsSendingMessage(true);

    try {
      // Add user message to UI immediately
      const userMessage = {
        content: messageText,
        type: "user",
        createdAt: new Date().toISOString(),
        id: Date.now(),
      };
      setMessages((prev) => [...prev, userMessage]);

      // Send message via SignalR
      await chatHubService.sendMessage(chatId, messageText);
      // Note: AI response will come via the SignalR ReceiveMessage event
    } catch (error) {
      console.error("Error sending message:", error);
      setIsSendingMessage(false);
      // You might want to show an error message or remove the user message
    }
  };

  if (!isUserLoggedIn) {
    return (
      <div className={styles.chatContainer}>
        <div className={styles.errorMessage}>Please log in to access chat.</div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className={styles.chatContainer}>
        <div className={styles.loadingMessage}>Loading chat...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.chatContainer}>
        <div className={styles.errorMessage}>{error}</div>
      </div>
    );
  }

  return (
    <div className={styles.chatContainer}>
      <div className={styles.messagesContainer}>
        {messages.map((msg) => (
          <div key={msg.id} className={`${styles.message} ${styles[msg.type]}`}>
            <div className={styles.messageContent}>{msg.content}</div>
          </div>
        ))}
        {isSendingMessage && (
          <div className={`${styles.message} ${styles.assistant}`}>
            <div className={styles.messageContent}>Thinking...</div>
          </div>
        )}
      </div>
      <ChatInput
        onSendMessage={handleSendMessage}
        disabled={isSendingMessage}
        placeholder={isSendingMessage ? "Sending..." : "Type your message..."}
      />
    </div>
  );
}
