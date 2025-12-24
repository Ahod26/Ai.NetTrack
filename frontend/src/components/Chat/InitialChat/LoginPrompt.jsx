import styles from "./InitialChat.module.css";

export default function LoginPrompt({ isSidebarOpen }) {
  return (
    <div className={styles.chatContainer}>
      <div className={styles.welcomeScreen}>
        <div className={styles.welcomeContent}>
          <h1 className={styles.welcomeTitle} style={{ display: "block" }}>
            Please log in to start chatting
          </h1>
        </div>
      </div>
    </div>
  );
}
