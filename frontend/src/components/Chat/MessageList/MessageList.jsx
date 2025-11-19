import { memo, useState, useEffect } from "react";
import { useSelector, useDispatch } from "react-redux";
import MarkdownRenderer from "../../MarkdownRenderer/MarkdownRenderer";
import TypingIndicator from "../TypingIndicator/TypingIndicator";
import { messagesSliceActions } from "../../../store/messagesSlice";
import { useStarSync } from "../../../hooks/useStarSync";
import styles from "./MessageList.module.css";

const MessageList = memo(function MessageList({
  messages,
  isSendingMessage,
  currentTool,
  messagesContainerRef,
  onScroll,
}) {
  const dispatch = useDispatch();
  const starredMessages = useSelector(
    (state) => state.messages.starredMessages
  );
  const [copiedMessageId, setCopiedMessageId] = useState(null);

  // Initialize star sync hook
  useStarSync();

  const hasChunkMessage = messages.some((msg) => msg.isChunkMessage);

  // Tool name to display message mapping
  const toolMessages = {
    "tavily-search": "🔍 Searching the internet...",
    get_file_contents: "📁 Fetching GitHub files...",
    list_files: "📂 Browsing GitHub repository...",
    search_code: "💻 Searching GitHub code...",
  };

  const getToolMessage = () => {
    if (currentTool && toolMessages[currentTool]) {
      return toolMessages[currentTool];
    }
    return "Thinking";
  };

  // Initialize starred messages from the messages prop on first load
  useEffect(() => {
    if (messages.length > 0) {
      const starredIds = messages
        .filter((msg) => msg.isStarred)
        .map((msg) => msg.id);

      if (starredIds.length > 0) {
        dispatch(messagesSliceActions.initializeStarredMessages(starredIds));
      }
    }
  }, [messages, dispatch]);

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

  const isMessageStarred = (messageId) => {
    return starredMessages.includes(messageId);
  };

  return (
    <div
      className={styles.messagesContainer}
      ref={messagesContainerRef}
      onScroll={onScroll}
    >
      {messages.map((msg) => (
        <div key={msg.id} className={`${styles.message} ${styles[msg.type]}`}>
          <div className={styles.messageContent}>
            {msg.type === "assistant" ? (
              <MarkdownRenderer content={msg.content} />
            ) : (
              msg.content
            )}
          </div>
          {msg.type === "assistant" && (
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
                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
                  </svg>
                )}
              </button>
            </div>
          )}
        </div>
      ))}
      {isSendingMessage && !hasChunkMessage && (
        <div className={`${styles.message} ${styles.assistant}`}>
          <div className={styles.messageContent}>
            <TypingIndicator label={getToolMessage()} />
          </div>
        </div>
      )}
    </div>
  );
});

export default MessageList;
