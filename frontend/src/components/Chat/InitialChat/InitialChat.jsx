import { useSelector } from "react-redux";
import { useInitialChatLogic } from "../../../hooks/useInitialChatLogic";
import ErrorPopup from "../../ErrorPopup";
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
    <div className={styles.chatContainer}>
      <ErrorPopup message={errorMessage} />
      <WelcomeMessage
        greeting={getPersonalizedGreeting()}
        onSendMessage={handleSendMessage}
        isCreatingChat={isCreatingChat}
        isSidebarOpen={isSidebarOpen}
      />
    </div>
  );
}
