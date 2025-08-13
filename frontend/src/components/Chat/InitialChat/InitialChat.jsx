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
      <WelcomeMessage
        greeting={getPersonalizedGreeting()}
        onSendMessage={handleSendMessage}
        isCreatingChat={isCreatingChat}
        isSidebarOpen={isSidebarOpen}
      />
    </div>
  );
}
