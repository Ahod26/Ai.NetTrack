import styles from "./NewsCard.module.css";
import { sourceTypeIcons, sourceTypeNames } from "../../shared";
import { useDateUtils } from "../../../../hooks/useDateUtils";

export default function NewsCard({ newsItem, onOpenModal }) {
  const { title, summary, url, sourceType, sourceName, publishedDate } =
    newsItem;

  const { formatRelativeDate } = useDateUtils();

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
    </article>
  );
}
