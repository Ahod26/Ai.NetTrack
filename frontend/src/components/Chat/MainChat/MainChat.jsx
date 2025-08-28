import { useEffect } from "react";
import { useParams } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { sidebarActions } from "../../../store/sidebarSlice";
import { useSignalRChat } from "../../../hooks/useSignalRChat";
import { useAutoScroll } from "../../../hooks/useAutoScroll";
import ErrorPopup from "../../ErrorPopup";
import ChatInput from "../ChatInput/ChatInput";
import MessageList from "../MessageList/MessageList";
import styles from "./MainChat.module.css";

export default function MainChat() {
  const { chatId } = useParams();
  const dispatch = useDispatch();
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  // Custom hooks for chat functionality
  const {
    messages,
    isLoading,
    error,
    isSendingMessage,
    sendMessage,
    errorMessage,
  } = useSignalRChat(chatId, isUserLoggedIn);

  const { messagesContainerRef, handleScroll } = useAutoScroll(messages);

  // Open sidebar when Chat component mounts
  useEffect(() => {
    dispatch(sidebarActions.openSidebar());
  }, [dispatch]);

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
    <div
      className={`${styles.chatContainer} ${
        isSidebarOpen ? styles.sidebarOpen : styles.sidebarClosed
      }`}
    >
      <ErrorPopup message={errorMessage} />
      <MessageList
        messages={messages}
        isSendingMessage={isSendingMessage}
        messagesContainerRef={messagesContainerRef}
        onScroll={handleScroll}
      />
      <ChatInput
        onSendMessage={sendMessage}
        disabled={isSendingMessage}
        placeholder={
          isSendingMessage
            ? "Wait for the response to finish"
            : "Type your message..."
        }
      />
    </div>
  );
}
