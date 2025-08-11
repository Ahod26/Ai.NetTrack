import { useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import ChatInput from "../ChatInput/ChatInput";
import styles from "./InitialChat.module.css";

export default function InitialChat() {
  const navigate = useNavigate();
  const { isUserLoggedIn, user } = useSelector((state) => state.userAuth);

  const handleSendMessage = (messageText) => {
    navigate("/chat/1", {
      replace: true,
      state: { initialMessage: messageText },
    });
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

  return (
    <div className={styles.chatContainer}>
      <div className={styles.welcomeScreen}>
        <div className={styles.welcomeContent}>
          <h1 className={styles.welcomeTitle}>{getPersonalizedGreeting()}</h1>
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
