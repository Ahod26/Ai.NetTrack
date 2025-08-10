import { useState } from "react";
import styles from "./Sidebar.module.css";

export default function Sidebar() {
  const [chats, setChats] = useState([
    {
      id: 1,
      title: "React Development Tips",
      time: "2 hours ago",
    },
    {
      id: 2,
      title: "CSS Grid Layout",
      time: "Yesterday",
    },
    {
      id: 3,
      title: "JavaScript Async/Await",
      time: "2 days ago",
    },
    {
      id: 4,
      title: "Node.js Best Practices",
      time: "3 days ago",
    },
    {
      id: 5,
      title: "Database Design",
      time: "1 week ago",
    },
  ]);

  const handleNewChat = () => {
    const newChat = {
      id: Date.now(),
      title: "New Chat",
      time: "Just now",
    };
    setChats([newChat, ...chats]);
  };

  return (
    <div className={styles.sidebar}>
      <div className={styles.header}>
        <h1 className={styles.title}>.NET AI Developer Hub</h1>
        <button className={styles.newChatBtn} onClick={handleNewChat}>
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
      </div>

      <div className={styles.chatHistory}>
        <div className={styles.sectionTitle}>Recent Chats</div>
        <div className={styles.chatList}>
          {chats.map((chat) => (
            <div key={chat.id} className={styles.chatItem}>
              <div className={styles.chatTitle}>{chat.title}</div>
              <div className={styles.chatTime}>{chat.time}</div>
            </div>
          ))}
        </div>
      </div>

      <div className={styles.quickActions}>
        <div className={styles.sectionTitle}>Quick Actions</div>
        <div className={styles.actionList}>
          <button className={styles.actionBtn}>
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14,2 14,8 20,8" />
            </svg>
            Documentation
          </button>
          <button className={styles.actionBtn}>
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
              <rect x="8" y="2" width="8" height="4" rx="1" ry="1" />
            </svg>
            Code Review
          </button>
          <button className={styles.actionBtn}>
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <circle cx="12" cy="12" r="3" />
              <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1 1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
            </svg>
            Settings
          </button>
        </div>
      </div>
    </div>
  );
}
