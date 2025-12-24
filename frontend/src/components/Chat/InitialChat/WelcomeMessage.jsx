import ChatInput from "../ChatInput/ChatInput";
import AnimatedGreeting from "../AnimatedGreeting/AnimatedGreeting";
import styles from "./InitialChat.module.css";

export default function WelcomeMessage({
  greeting,
  onSendMessage,
  isCreatingChat,
  isSidebarOpen,
}) {
  return (
    <div
      className={`${styles.welcomeScreen} ${
        isSidebarOpen ? styles.sidebarOpen : ""
      }`}
    >
      <div className={styles.welcomeContent}>
        <AnimatedGreeting />
        <div className={styles.welcomeInputContainer}>
          <ChatInput
            onSendMessage={onSendMessage}
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
  );
}
