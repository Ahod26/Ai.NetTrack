import ChatInput from "../ChatInput/ChatInput";
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
        <h1 className={styles.welcomeTitle}>{greeting}</h1>
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
