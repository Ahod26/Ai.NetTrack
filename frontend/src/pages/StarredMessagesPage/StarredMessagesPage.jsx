import { useState, useEffect, memo } from "react";
import { useSelector, useDispatch } from "react-redux";
import Sidebar from "../../components/Sidebar/Sidebar";
import MarkdownRenderer from "../../components/MarkdownRenderer/MarkdownRenderer";
import { messagesSliceActions } from "../../store/messagesSlice";
import { sidebarActions } from "../../store/sidebarSlice";
import { getAllStarredMessages } from "../../api/messages";
import { useStarSync } from "../../hooks/useStarSync";
import styles from "./StarredMessagesPage.module.css";

const StarredMessagesPage = memo(function StarredMessagesPage() {
  const dispatch = useDispatch();
  const starredMessages = useSelector(
    (state) => state.messages.starredMessages
  );
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);
  const [messages, setMessages] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [copiedMessageId, setCopiedMessageId] = useState(null);

  // Initialize star sync hook
  useStarSync();

  // Open sidebar when component mounts (same as MainChat)
  useEffect(() => {
    dispatch(sidebarActions.openSidebar());
  }, [dispatch]);

  useEffect(() => {
    const fetchStarredMessages = async () => {
      try {
        setIsLoading(true);
        const response = await getAllStarredMessages();
        setMessages(response);

        // Initialize Redux store with starred message IDs
        const starredIds = response.map((msg) => msg.id);
        dispatch(messagesSliceActions.initializeStarredMessages(starredIds));
      } catch (error) {
        console.error("Failed to fetch starred messages:", error);
        setMessages([]);
      } finally {
        setIsLoading(false);
      }
    };

    fetchStarredMessages();
  }, [dispatch]);

  const handleStarToggle = (messageId) => {
    // Optimistic update 
    dispatch(messagesSliceActions.toggleMessageStarOptimistic(messageId));
  };

  const handleCopyMessage = async (content, messageId) => {
    try {
      await navigator.clipboard.writeText(content);
      setCopiedMessageId(messageId);

      // Reset copy state after 2 seconds
      setTimeout(() => {
        setCopiedMessageId(null);
      }, 2000);
    } catch (error) {
      console.error("Failed to copy message:", error);
    }
  };

  const handleFeedback = (messageId) => {
    // Placeholder for future feedback functionality
    console.log("Feedback for message:", messageId);
  };

  // Check if message is starred (use Redux state for optimistic updates)
  const isMessageStarred = (messageId) => {
    return starredMessages.includes(messageId);
  };

  if (isLoading) {
    return (
      <div className={styles.pageContainer}>
        <Sidebar />
        <div
          className={`${styles.chatContainer} ${
            isSidebarOpen ? styles.sidebarOpen : styles.sidebarClosed
          }`}
        >
          <div className={styles.loadingMessage}>
            Loading starred messages...
          </div>
        </div>
      </div>
    );
  }

  if (messages.length === 0) {
    return (
      <div className={styles.pageContainer}>
        <Sidebar />
        <div
          className={`${styles.chatContainer} ${
            isSidebarOpen ? styles.sidebarOpen : styles.sidebarClosed
          }`}
        >
          <div className={styles.emptyState}>
            <div className={styles.emptyIcon}>
              <svg
                width="64"
                height="64"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
              >
                <polygon points="12,2 15.09,8.26 22,9.27 17,14.14 18.18,21.02 12,17.77 5.82,21.02 7,14.14 2,9.27 8.91,8.26" />
              </svg>
            </div>
            <h2 className={styles.emptyTitle}>No starred messages yet</h2>
            <p className={styles.emptyDescription}>
              Start conversations and star helpful AI responses to save them
              here.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.pageContainer}>
      <Sidebar />
      <div
        className={`${styles.chatContainer} ${
          isSidebarOpen ? styles.sidebarOpen : styles.sidebarClosed
        }`}
      >
        <div className={styles.messagesContainer}>
          {messages.map((msg, index) => (
            <div key={msg.id}>
              <div className={`${styles.message} ${styles.assistant}`}>
                <div className={styles.messageContent}>
                  <MarkdownRenderer content={msg.content} />
                </div>
                <div className={styles.messageActions}>
                  <button
                    className={`${styles.actionButton} ${styles.starButton} ${
                      isMessageStarred(msg.id) ? styles.starred : ""
                    }`}
                    onClick={() => handleStarToggle(msg.id)}
                    title={
                      isMessageStarred(msg.id)
                        ? "Remove from starred"
                        : "Add to starred"
                    }
                  >
                    <svg
                      width="16"
                      height="16"
                      viewBox="0 0 24 24"
                      fill={isMessageStarred(msg.id) ? "#fbbf24" : "none"}
                      stroke="currentColor"
                      strokeWidth="2"
                    >
                      <polygon points="12,2 15.09,8.26 22,9.27 17,14.14 18.18,21.02 12,17.77 5.82,21.02 7,14.14 2,9.27 8.91,8.26" />
                    </svg>
                  </button>
                  <button
                    className={`${styles.actionButton} ${styles.feedbackButton}`}
                    onClick={() => handleFeedback(msg.id)}
                    title="Provide feedback on this response"
                  >
                    <svg
                      width="16"
                      height="16"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                    >
                      <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
                    </svg>
                  </button>
                  <button
                    className={`${styles.actionButton} ${styles.copyButton} ${
                      copiedMessageId === msg.id ? styles.copied : ""
                    }`}
                    onClick={() => handleCopyMessage(msg.content, msg.id)}
                    title={
                      copiedMessageId === msg.id
                        ? "Message copied!"
                        : "Copy message to clipboard"
                    }
                  >
                    {copiedMessageId === msg.id ? (
                      <svg
                        width="16"
                        height="16"
                        viewBox="0 0 1920 1920"
                        fill="currentColor"
                      >
                        <path
                          d="M1743.858 267.012 710.747 1300.124 176.005 765.382 0 941.387l710.747 710.871 1209.24-1209.116z"
                          fillRule="evenodd"
                        />
                      </svg>
                    ) : (
                      <svg
                        width="16"
                        height="16"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                      >
                        <rect
                          x="9"
                          y="9"
                          width="13"
                          height="13"
                          rx="2"
                          ry="2"
                        />
                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
                      </svg>
                    )}
                  </button>
                </div>
              </div>
              {index < messages.length - 1 && (
                <div className={styles.messageDivider}></div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
});

export default StarredMessagesPage;
