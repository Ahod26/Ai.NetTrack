import { useSelector } from "react-redux";
import { useInitialChatLogic } from "../../../hooks/useInitialChatLogic";
import WelcomeMessage from "./WelcomeMessage";
import LoginPrompt from "./LoginPrompt";
import styles from "./InitialChat.module.css";

export default function InitialChat() {
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);
  const {
    isUserLoggedIn,
    isCreatingChat,
    handleSendMessage,
    getPersonalizedGreeting,
    errorMessage,
  } = useInitialChatLogic();

  if (!isUserLoggedIn) {
    return <LoginPrompt isSidebarOpen={isSidebarOpen} />;
  }

  return (
    <div
      className={`${styles.chatContainer} ${
        !isSidebarOpen ? styles.sidebarClosed : ""
      }`}
    >
      {errorMessage && (
        <div className={styles.errorBox}>
          {errorMessage}
          <div className={styles.timerBar}></div>
        </div>
      )}
      <WelcomeMessage
        greeting={getPersonalizedGreeting()}
        onSendMessage={handleSendMessage}
        isCreatingChat={isCreatingChat}
        isSidebarOpen={isSidebarOpen}
      />
    </div>
  );
}
