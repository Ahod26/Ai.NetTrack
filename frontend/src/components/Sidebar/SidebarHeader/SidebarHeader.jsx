import { Link, useLocation } from "react-router-dom";
import styles from "../Sidebar.module.css";

export default function SidebarHeader({ isUserLoggedIn, onNewChat }) {
  const location = useLocation();
  const isStarredPage = location.pathname === "/chat/starred";

  return (
    <div className={styles.header}>
      <h1 className={styles.title}>.Net AI Hub</h1>
      {isUserLoggedIn && (
        <div className={styles.headerButtons}>
          <button className={styles.newChatBtn} onClick={onNewChat}>
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            New Chat
          </button>

          <Link
            to="/chat/starred"
            className={`${styles.starredBtn} ${
              isStarredPage ? styles.starredBtnActive : ""
            }`}
          >
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
            Starred Messages
          </Link>
        </div>
      )}
    </div>
  );
}
