import { useEffect, memo } from "react";
import { useParams } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { sidebarActions } from "../../../store/sidebarSlice";
import { useSignalRChat } from "../../../hooks/useSignalRChat";
import { useAutoScroll } from "../../../hooks/useAutoScroll";
import { MessageListSkeleton } from "../../Skeleton";
import ErrorPopup from "../../ErrorPopup";
import ChatInput from "../ChatInput/ChatInput";
import MessageList from "../MessageList/MessageList";
import styles from "./MainChat.module.css";

const MainChat = memo(function MainChat() {
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
    cancelGeneration,
    errorMessage,
    currentTool,
  } = useSignalRChat(chatId, isUserLoggedIn);

  const { messagesContainerRef, handleScroll, scrollToBottom } =
    useAutoScroll(messages);

  // Open sidebar when Chat component mounts
  useEffect(() => {
    dispatch(sidebarActions.openSidebar());
  }, [dispatch]);

  // Scroll to bottom when chat changes
  useEffect(() => {
    if (!isLoading && messages.length > 0) {
      setTimeout(() => {
        scrollToBottom();
      }, 50);
    }
  }, [chatId, isLoading, messages.length, scrollToBottom]);

  if (!isUserLoggedIn) {
    return (
      <div className={styles.chatContainer}>
        <div className={styles.errorMessage}>Please log in to access chat.</div>
      </div>
    );
  }

  if (isLoading) {
    // Preserve full layout so skeleton occupies exact future positions
    return (
      <div className={styles.chatContainer}>
        <div className={styles.messagesContainer}>
          <MessageListSkeleton inline count={3} />
        </div>
        <ChatInput
          onSendMessage={() => {}}
          disabled={true}
          placeholder="Loading chat..."
        />
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
    <div className={styles.chatContainer}>
      <ErrorPopup message={errorMessage} />
      <MessageList
        messages={messages}
        isSendingMessage={isSendingMessage}
        currentTool={currentTool}
        messagesContainerRef={messagesContainerRef}
        onScroll={handleScroll}
      />
      <ChatInput
        onSendMessage={sendMessage}
        onCancel={cancelGeneration}
        isGenerating={isSendingMessage}
        disabled={isSendingMessage}
        placeholder={
          isSendingMessage
            ? "Wait for the response to finish"
            : "Type your message..."
        }
      />
    </div>
  );
});

export default MainChat;
