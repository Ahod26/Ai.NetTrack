import { memo, useState, useEffect } from "react";
import { useSelector, useDispatch } from "react-redux";
import MarkdownRenderer from "../../MarkdownRenderer/MarkdownRenderer";
import TypingIndicator from "../TypingIndicator/TypingIndicator";
import ReportModal from "../../ReportModal/ReportModal";
import { messagesSliceActions } from "../../../store/messagesSlice";
import { toggleMessageStar, reportMessage } from "../../../api/messages";
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
  const reportedMessages = useSelector(
    (state) => state.messages.reportedMessages
  );
  const [copiedMessageId, setCopiedMessageId] = useState(null);
  const [loadingStars, setLoadingStars] = useState(new Set());
  const [reportModalMessageId, setReportModalMessageId] = useState(null);

  const hasChunkMessage = messages.some((msg) => msg.isChunkMessage);

  // Tool name to display message mapping
  const toolDisplayNames = {
    "tavily-search": "Searching the internet...",
    get_file_contents: "Fetching GitHub files...",
    list_files: "Browsing GitHub repository...",
    search_code: "Searching GitHub code...",
  };

  const getToolMessage = () => {
    if (currentTool && toolDisplayNames[currentTool]) {
      return toolDisplayNames[currentTool];
    }
    return "Thinking";
  };

  useEffect(() => {
    if (messages.length > 0) {
      const starredIds = messages
        .filter((msg) => msg.isStarred)
        .map((msg) => msg.id);
      dispatch(messagesSliceActions.setStarredMessages(starredIds));

      const reportedIds = messages
        .filter((msg) => msg.isReported)
        .map((msg) => msg.id);
      dispatch(messagesSliceActions.setReportedMessages(reportedIds));
    }
  }, [messages, dispatch]);

  const handleStarToggle = async (messageId) => {
    // Ignore if already processing this message
    if (loadingStars.has(messageId)) {
      return;
    }

    // Add to loading set
    setLoadingStars((prev) => new Set(prev).add(messageId));

    // Optimistic update - toggle immediately
    dispatch(messagesSliceActions.toggleMessageStar(messageId));

    try {
      await toggleMessageStar(messageId);
      // Success - keep the toggled state
    } catch (error) {
      console.error("Failed to toggle star:", error);
      // Revert the toggle on error
      dispatch(messagesSliceActions.toggleMessageStar(messageId));
    } finally {
      // Remove from loading set
      setLoadingStars((prev) => {
        const newSet = new Set(prev);
        newSet.delete(messageId);
        return newSet;
      });
    }
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
    // Don't allow reporting already reported messages
    if (reportedMessages.includes(messageId)) {
      return;
    }
    setReportModalMessageId(messageId);
  };

  const handleReportSubmit = async (messageId, reportText) => {
    await reportMessage(messageId, reportText);
    // Mark message as reported in state
    dispatch(messagesSliceActions.markMessageReported(messageId));
  };

  const isMessageReported = (messageId) => {
    return reportedMessages.includes(messageId);
  };

  const handleCloseReportModal = () => {
    setReportModalMessageId(null);
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
                className={`${styles.actionButton} ${styles.feedbackButton} ${
                  isMessageReported(msg.id) ? styles.reported : ""
                }`}
                onClick={() => handleFeedback(msg.id)}
                title={
                  isMessageReported(msg.id)
                    ? "Feedback sent for this message"
                    : "Provide feedback on this response"
                }
                disabled={isMessageReported(msg.id)}
              >
                {isMessageReported(msg.id) ? (
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                  >
                    <polyline points="20 6 9 17 4 12" />
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
                    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
                  </svg>
                )}
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
      {reportModalMessageId && (
        <ReportModal
          messageId={reportModalMessageId}
          onClose={handleCloseReportModal}
          onSubmit={handleReportSubmit}
        />
      )}
    </div>
  );
});

export default MessageList;
