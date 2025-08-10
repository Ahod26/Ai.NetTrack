import { useState, useEffect } from "react";
import { useLocation } from "react-router-dom";
import ChatInput from "../ChatInput/ChatInput";
import styles from "./Chat.module.css";

export default function Chat() {
  const [messages, setMessages] = useState([]);
  const [hasProcessedInitialMessage, setHasProcessedInitialMessage] = useState(false);
  const location = useLocation();

  // Handle initial message from navigation state
  useEffect(() => {
    if (location.state?.initialMessage && !hasProcessedInitialMessage) {
      const initialMessage = {
        text: location.state.initialMessage,
        sender: "user",
      };
      setMessages([initialMessage]);
      setHasProcessedInitialMessage(true);

      // Add AI response after initial message
      setTimeout(() => {
        const aiMessage = {
          text: "I'm here to help! This is a placeholder response.",
          sender: "ai",
        };
        setMessages((prev) => [...prev, aiMessage]);
      }, 1000);
    }
  }, [location.state, hasProcessedInitialMessage]);

  const handleSendMessage = (messageText) => {
    const newMessage = { text: messageText, sender: "user" };
    const updatedMessages = [...messages, newMessage];
    setMessages(updatedMessages);

    // Simulate AI response
    setTimeout(() => {
      const aiMessage = {
        text: "I'm here to help! This is a placeholder response.",
        sender: "ai",
      };
      setMessages((prev) => [...prev, aiMessage]);
    }, 1000);
  };

  return (
    <div className={styles.chatContainer}>
      <div className={styles.messagesContainer}>
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`${styles.message} ${styles[msg.sender]}`}
          >
            <div className={styles.messageContent}>{msg.text}</div>
          </div>
        ))}
      </div>
      <ChatInput onSendMessage={handleSendMessage} />
    </div>
  );
}
