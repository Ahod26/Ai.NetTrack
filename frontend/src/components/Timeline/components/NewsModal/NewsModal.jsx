import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import styles from "./NewsModal.module.css";
import { sourceTypeIcons, sourceTypeNames } from "../../shared";
import { useDateUtils } from "../../../../hooks/useDateUtils";
import { createChat } from "../../../../api/chat";
import { chatSliceActions } from "../../../../store/chat";
import chatHubService from "../../../../api/chatHub";

export default function NewsModal({ newsItem, isOpen, onClose, onChatError }) {
  const {
    title,
    content,
    summary,
    url,
    imageUrl,
    sourceType,
    sourceName,
    publishedDate,
  } = newsItem || {};

  const { formatFullDate } = useDateUtils();
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);
  const [isCreatingChat, setIsCreatingChat] = useState(false);

  useEffect(() => {
    const handleEscape = (e) => {
      if (e.key === "Escape") {
        onClose();
      }
    };

    const handleBodyScroll = () => {
      if (isOpen) {
        document.body.style.overflow = "hidden";
      } else {
        document.body.style.overflow = "unset";
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
      handleBodyScroll();
    }

    return () => {
      document.removeEventListener("keydown", handleEscape);
      document.body.style.overflow = "unset";
    };
  }, [isOpen, onClose]);

  const handleBackdropClick = (e) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  const handleExternalLink = () => {
    if (url) {
      window.open(url, "_blank", "noopener,noreferrer");
    }
  };

  const handleAskChat = async () => {
    if (!isUserLoggedIn || !url || isCreatingChat) {
      return;
    }

    setIsCreatingChat(true);

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

      // Close modal and navigate to the new chat
      onClose();
      navigate(`/chat/${newChat.id}`, {
        replace: true,
        state: { initialMessage },
      });

      // Join the chat via SignalR
      await chatHubService.joinChat(newChat.id);

      // Send the initial message
      await chatHubService.sendMessage(newChat.id, initialMessage);
    } catch (error) {
      setIsCreatingChat(false);
      // Close modal first
      onClose();

      // Display error message using the error handler
      const errorMsg = error?.message || "Failed to create chat";
      if (onChatError) {
        onChatError(errorMsg);
      }
    }
  };

  const getButtonText = () => {
    switch (sourceType) {
      case 1:
        return "Go to Repository";
      case 2:
        return "Read Article";
      case 3:
        return "Watch Video";
      default:
        return "View Source";
    }
  };

  // Show Ask Chat button for GitHub (1), RSS/Blog (2), and YouTube (3) sources
  const showAskChatButton =
    sourceType === 1 || sourceType === 2 || sourceType === 3;

  if (!isOpen || !newsItem) return null;

  return (
    <div className={styles.modalOverlay} onClick={handleBackdropClick}>
      <div className={styles.modalContent}>
        <header className={styles.modalHeader}>
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
            <div className={styles.sourceDetails}>
              <span className={styles.sourceName}>
                {sourceName || sourceTypeNames[sourceType] || "Unknown"}
              </span>
              <time className={styles.publishedDate} dateTime={publishedDate}>
                {formatFullDate(publishedDate)}
              </time>
            </div>
          </div>
          <button
            className={styles.closeButton}
            onClick={onClose}
            aria-label="Close modal"
          >
            ✕
          </button>
        </header>

        <div className={styles.modalBody}>
          <h1 className={styles.title}>{title || "Untitled"}</h1>

          {summary && (
            <div className={styles.summarySection}>
              <h2 className={styles.sectionTitle}>Summary</h2>
              <div className={styles.summaryContent}>
                <p className={styles.summary}>{summary}</p>
                {imageUrl && sourceType === 3 && (
                  <div className={styles.thumbnailContainer}>
                    <img
                      src={imageUrl}
                      alt={title || "Video thumbnail"}
                      className={styles.youtubeThumbnail}
                      width="120"
                      height="90"
                      onError={(e) => {
                        e.target.style.display = "none";
                      }}
                    />
                  </div>
                )}
              </div>
            </div>
          )}

          {content && (
            <div className={styles.contentSection}>
              <h2 className={styles.sectionTitle}>Content</h2>
              <div className={styles.content}>
                <p className={styles.contentText}>{content}</p>
              </div>
            </div>
          )}
        </div>

        <footer className={styles.modalFooter}>
          {showAskChatButton && isUserLoggedIn && (
            <button
              className={styles.askChatButton}
              onClick={handleAskChat}
              disabled={isCreatingChat}
            >
              {isCreatingChat ? (
                <>
                  <span className={styles.spinner}></span>
                  <span>Creating...</span>
                </>
              ) : (
                <span>Ask Chat</span>
              )}
            </button>
          )}
          {url && (
            <button
              className={styles.externalLinkButton}
              onClick={handleExternalLink}
            >
              <span>{getButtonText()}</span>
              <span className={styles.externalIcon}></span>
            </button>
          )}
          <button className={styles.closeButtonSecondary} onClick={onClose}>
            Close
          </button>
        </footer>
      </div>
    </div>
  );
}
