import { useState, useEffect } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { userAuthSliceAction } from "../../store/userAuth";
import { logoutUser } from "../../api/auth";
import { getUserChats } from "../../api/chat";
import styles from "./Sidebar.module.css";

export default function Sidebar() {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { chatId: currentChatId } = useParams(); // Get current chat ID from URL
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);
  const { refreshTrigger } = useSelector((state) => state.chat);

  const [chats, setChats] = useState([]);
  const [isLoadingChats, setIsLoadingChats] = useState(false);

  // Fetch user chats when component mounts or user logs in
  useEffect(() => {
    const fetchUserChats = async () => {
      try {
        setIsLoadingChats(true);
        const userChats = await getUserChats();
        console.log("Fetched chats:", userChats);

        // Transform the data to match the sidebar format
        const formattedChats = userChats.map((chat) => ({
          id: chat.Id || chat.id,
          title: chat.Title || chat.title,
          time: formatChatTime(chat.CreatedAt || chat.createdAt),
        }));

        setChats(formattedChats);
      } catch (error) {
        console.error("Error fetching chats:", error);
      } finally {
        setIsLoadingChats(false);
      }
    };

    if (isUserLoggedIn) {
      fetchUserChats();
    } else {
      setChats([]);
    }
  }, [isUserLoggedIn, refreshTrigger]);

  const formatChatTime = (dateString) => {
    if (!dateString) return "Unknown";

    const chatDate = new Date(dateString);
    const now = new Date();
    const diffInHours = Math.floor((now - chatDate) / (1000 * 60 * 60));

    if (diffInHours < 1) {
      return "Just now";
    } else if (diffInHours < 24) {
      return `${diffInHours} hour${diffInHours > 1 ? "s" : ""} ago`;
    } else if (diffInHours < 48) {
      return "Yesterday";
    } else {
      const diffInDays = Math.floor(diffInHours / 24);
      return `${diffInDays} day${diffInDays > 1 ? "s" : ""} ago`;
    }
  };

  const handleNewChat = () => {
    navigate("/chat/new");
  };

  const handleLogout = async () => {
    try {
      await logoutUser();
      dispatch(userAuthSliceAction.setUserLoggedOut());
      navigate("/chat/new");
    } catch (error) {
      console.error("Logout error:", error);
    }
  };

  return (
    <div className={styles.sidebar}>
      <div className={styles.header}>
        <h1 className={styles.title}>.NET AI Developer Hub</h1>
        {isUserLoggedIn && (
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
        )}
      </div>

      {isUserLoggedIn && (
        <div className={styles.chatHistory}>
          <div className={styles.sectionTitle}>Recent Chats</div>
          <div className={styles.chatList}>
            {isLoadingChats ? (
              <div className={styles.loadingChats}>Loading chats...</div>
            ) : chats.length > 0 ? (
              chats.map((chat) => (
                <Link
                  key={chat.id}
                  to={`/chat/${chat.id}`}
                  className={`${styles.chatItem} ${
                    currentChatId === chat.id.toString()
                      ? styles.activeChatItem
                      : ""
                  }`}
                >
                  <div className={styles.chatTitle}>{chat.title}</div>
                  <div className={styles.chatTime}>{chat.time}</div>
                </Link>
              ))
            ) : (
              <div className={styles.noChats}>
                No chats yet. Start a new conversation!
              </div>
            )}
          </div>
        </div>
      )}

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
