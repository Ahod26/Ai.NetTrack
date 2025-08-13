import styles from "./InitialChat.module.css";

export default function LoginPrompt({ isSidebarOpen }) {
  return (
    <div
      className={`${styles.chatContainer} ${
        !isSidebarOpen ? styles.sidebarClosed : ""
      }`}
    >
      <div
        className={`${styles.welcomeScreen} ${
          isSidebarOpen ? styles.sidebarOpen : ""
        }`}
      >
        <div className={styles.welcomeContent}>
          <h1 className={styles.welcomeTitle}>
            Please log in to start chatting
          </h1>
        </div>
      </div>
    </div>
  );
}
