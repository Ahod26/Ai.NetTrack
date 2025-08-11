import { useState } from "react";
import { Link } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { userAuthSliceAction } from "../../store/userAuth";
import { logoutUser } from "../../api/auth";
import styles from "./Sidebar.module.css";

export default function Sidebar() {
  const dispatch = useDispatch();
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);

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

  const handleLogout = async () => {
    try {
      await logoutUser();
      dispatch(userAuthSliceAction.setUserLoggedOut());
    } catch (error) {
      console.error("Logout error:", error);
    }
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

      <div className={styles.userSection}>
        <div className={styles.sectionTitle}>Account</div>
        {isUserLoggedIn ? (
          // Logged in state
          <div className={styles.userActions}>
            <Link to="/account" className={styles.accountBtn}>
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
              Account Settings
            </Link>
            <button className={styles.logoutBtn} onClick={handleLogout}>
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                <polyline points="16,17 21,12 16,7" />
                <path d="M21 12H9" />
              </svg>
              Logout
            </button>
          </div>
        ) : (
          // Not logged in state
          <div className={styles.authButtons}>
            <Link to="/login" className={styles.loginBtn}>
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" />
                <polyline points="10,17 15,12 10,7" />
                <path d="M15 12H3" />
              </svg>
              Login
            </Link>
            <Link to="/signup" className={styles.signupBtn}>
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
              Sign Up
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}
