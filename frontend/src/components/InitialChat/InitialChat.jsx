import { useNavigate } from "react-router-dom";
import ChatInput from "../ChatInput/ChatInput";
import styles from "./InitialChat.module.css";

export default function InitialChat() {
  const navigate = useNavigate();

  const handleSendMessage = (messageText) => {
    navigate("/chat/1", {
      replace: true,
      state: { initialMessage: messageText },
    });
  };

  return (
    <div className={styles.chatContainer}>
      <div className={styles.welcomeScreen}>
        <div className={styles.welcomeContent}>
          <h1 className={styles.welcomeTitle}>How can I help you today?</h1>
          <div className={styles.welcomeInputContainer}>
            <ChatInput
              onSendMessage={handleSendMessage}
              placeholder="Tell me what you'd like to build or ask me anything..."
            />
          </div>
        </div>
      </div>
    </div>
  );
}
