import { Link, useLocation } from "react-router-dom";
import styles from "../Sidebar.module.css";

export default function SidebarHeader({ isUserLoggedIn, onNewChat }) {
  const location = useLocation();
  const isStarredPage = location.pathname === "/chat/starred";

  return (
    <div className={styles.header}>
      <h1 className={styles.title}>.NET AI Developer Hub</h1>
      {isUserLoggedIn && (
        <div className={styles.headerButtons}>
          <button className={styles.newChatBtn} onClick={onNewChat}>
            <div className={styles.newChatIcon}>
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M12 5v14M5 12h14" />
              </svg>
            </div>
            New Chat
          </button>

          <Link
            to="/chat/starred"
            className={`${styles.starredBtn} ${
              isStarredPage ? styles.starredBtnActive : ""
            }`}
          >
            <div className={styles.starredIcon}>
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill={isStarredPage ? "currentColor" : "none"}
                stroke="currentColor"
                strokeWidth="2"
              >
                <polygon points="12,2 15.09,8.26 22,9.27 17,14.14 18.18,21.02 12,17.77 5.82,21.02 7,14.14 2,9.27 8.91,8.26" />
              </svg>
            </div>
            Starred Messages
          </Link>
        </div>
      )}
    </div>
  );
}
