import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { createChat } from "../../api/chat";
import chatHubService from "../../api/chatHub";
import { chatSliceActions } from "../../store/chat";
import ChatInput from "../ChatInput/ChatInput";
import styles from "./InitialChat.module.css";

export default function InitialChat() {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { isUserLoggedIn, user } = useSelector((state) => state.userAuth);
  const [isCreatingChat, setIsCreatingChat] = useState(false);

  const handleSendMessage = async (messageText) => {
    if (!isUserLoggedIn || isCreatingChat) {
      return;
    }

    setIsCreatingChat(true);

    try {
      // Generate a simple title for now - you can make this AI-generated later
      const chatCounter = Date.now() % 1000; 
      const chatTitle = `Chat ${chatCounter}`;

      // Create new chat via REST API
      const newChat = await createChat(chatTitle);
      console.log("Created new chat:", newChat);

      // Join the chat via SignalR
      await chatHubService.joinChat(newChat.id);

      // Send the initial message
      await chatHubService.sendMessage(newChat.id, messageText);

      // Trigger chat list refresh in sidebar
      dispatch(chatSliceActions.triggerChatRefresh());

      // Navigate to the new chat
      navigate(`/chat/${newChat.id}`, {
        replace: true,
      });
    } catch (error) {
      console.error("Error creating chat or sending message:", error);
      setIsCreatingChat(false);
      // You might want to show an error message to the user here
    }
  };

  // Extract first name from full name
  const getFirstName = () => {
    if (!isUserLoggedIn || !user?.userName) {
      return "";
    }

    const firstName = user.userName.split(" ")[0];
    return firstName;
  };

  const getPersonalizedGreeting = () => {
    const firstName = getFirstName();
    if (firstName) {
      return `How can I help you today, ${firstName}?`;
    }
    return "How can I help you today?";
  };

  if (!isUserLoggedIn) {
    return (
      <div className={styles.chatContainer}>
        <div className={styles.welcomeScreen}>
          <div className={styles.welcomeContent}>
            <h1 className={styles.welcomeTitle}>
              Please log in to start chatting
            </h1>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.chatContainer}>
      <div className={styles.welcomeScreen}>
        <div className={styles.welcomeContent}>
          <h1 className={styles.welcomeTitle}>{getPersonalizedGreeting()}</h1>
          <div className={styles.welcomeInputContainer}>
            <ChatInput
              onSendMessage={handleSendMessage}
              placeholder={
                isCreatingChat
                  ? "Creating chat..."
                  : "Tell me what you'd like to build or ask me anything..."
              }
              disabled={isCreatingChat}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
