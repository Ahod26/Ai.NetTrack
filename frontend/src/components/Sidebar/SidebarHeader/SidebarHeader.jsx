import styles from "../Sidebar.module.css";

export default function SidebarHeader({ isUserLoggedIn, onNewChat }) {
  return (
    <div className={styles.header}>
      <h1 className={styles.title}>.NET AI Developer Hub</h1>
      {isUserLoggedIn && (
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
      )}
    </div>
  );
}
