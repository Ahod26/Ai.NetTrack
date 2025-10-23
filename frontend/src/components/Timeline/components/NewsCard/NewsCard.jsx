import { useNavigate } from "react-router-dom";
import styles from "./NewsCard.module.css";
import { sourceTypeIcons, sourceTypeNames } from "../../shared";
import { useDateUtils } from "../../../../hooks/useDateUtils";
import { createChat } from "../../../../api/chat";
import { useSelector, useDispatch } from "react-redux";
import { chatSliceActions } from "../../../../store/chat";
import chatHubService from "../../../../api/chatHub";

export default function NewsCard({ newsItem, onOpenModal }) {
  const { title, summary, url, sourceType, sourceName, publishedDate } =
    newsItem;

  const { formatRelativeDate } = useDateUtils();
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);

  const handleCardClick = () => {
    onOpenModal(newsItem);
  };

  const handleKeyDown = (e) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      handleCardClick();
    }
  };

  const handleExternalLink = (e) => {
    e.stopPropagation();
    if (url) {
      window.open(url, "_blank", "noopener,noreferrer");
    }
  };

  const handleAskChat = async (e) => {
    e.stopPropagation();

    if (!isUserLoggedIn || !url) {
      return;
    }

    try {
      let initialMessage;

      if (sourceType === 1) {
        // GitHub repository
        initialMessage = `Summarize the latest release from this repository: ${url}`;
      } else if (sourceType === 3) {
        // YouTube video
        initialMessage = "Summarize this YouTube video";
      } else {
        // RSS/Blog article
        initialMessage = "Summarize this article";
      }

      const newChat = await createChat(initialMessage, null, url);

      const chatTitle =
        title && title.length > 50
          ? title.slice(0, 50) + "..."
          : title || initialMessage;

      // Add chat optimistically to Redux store
      dispatch(
        chatSliceActions.addChat({
          id: newChat.id,
          title: chatTitle,
          time: "Just now",
          lastMessageAt: new Date().toISOString(),
        })
      );

      // Navigate to the new chat immediately
      navigate(`/chat/${newChat.id}`, {
        replace: true,
        state: { initialMessage },
      });

      // Join the chat via SignalR
      await chatHubService.joinChat(newChat.id);

      // Send the initial message
      await chatHubService.sendMessage(newChat.id, initialMessage);
    } catch (error) {
      console.error("Error creating chat:", error);
    }
  };

  // Show Ask Chat button for GitHub (1), RSS/Blog (2), and YouTube (3) sources
  const showAskChatButton =
    sourceType === 1 || sourceType === 2 || sourceType === 3;

  return (
    <article
      className={`${styles.newsCard} ${styles.clickable}`}
      onClick={handleCardClick}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="button"
      aria-label={`Read article: ${title}`}
    >
      <div className={styles.cardHeader}>
        <div className={styles.sourceInfo}>
          <img
            src={
              sourceTypeIcons[sourceType] ||
              "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' fill='%23666' viewBox='0 0 24 24'%3E%3Cpath d='M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z'/%3E%3C/svg%3E"
            }
            alt={sourceTypeNames[sourceType] || "Unknown"}
            className={styles.sourceIcon}
            onError={(e) => {
              e.target.style.display = "none";
            }}
          />
          <span className={styles.sourceName}>
            {sourceName || sourceTypeNames[sourceType] || "Unknown"}
          </span>
        </div>
        <time className={styles.publishedDate} dateTime={publishedDate}>
          {formatRelativeDate(publishedDate)}
        </time>
      </div>

      <div className={styles.cardContent}>
        <h2 className={styles.title}>{title || "Untitled"}</h2>

        {summary && <p className={styles.summary}>{summary}</p>}
      </div>

      <div className={styles.cardFooter}>
        <span className={styles.readMore}>Click to read more →</span>
        <div className={styles.actionButtons}>
          {showAskChatButton && isUserLoggedIn && (
            <button
              className={styles.askChatButton}
              onClick={handleAskChat}
              aria-label="Ask chat about this content"
            >
              💬 Ask Chat
            </button>
          )}
          {url && (
            <button
              className={styles.externalLinkButton}
              onClick={handleExternalLink}
              aria-label="Open original article"
            >
              🔗 Source
            </button>
          )}
        </div>
      </div>
    </article>
  );
}
