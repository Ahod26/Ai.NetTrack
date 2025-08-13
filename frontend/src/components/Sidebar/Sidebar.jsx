import { useState, useEffect } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { userAuthSliceAction } from "../../store/userAuth";
import { logoutUser } from "../../api/auth";
import { getUserChats, deleteChatById } from "../../api/chat";
import { chatSliceActions } from "../../store/chat";
import { sidebarActions } from "../../store/sidebarSlice";
import styles from "./Sidebar.module.css";

export default function Sidebar() {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { chatId: currentChatId } = useParams(); // Get current chat ID from URL
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);
  const { refreshTrigger } = useSelector((state) => state.chat);
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  const [chats, setChats] = useState([]);
  const [isLoadingChats, setIsLoadingChats] = useState(false);
  const [openDropdown, setOpenDropdown] = useState(null); // Track which dropdown is open
  const [deleteModal, setDeleteModal] = useState({
    isOpen: false,
    chatId: null,
  }); // Delete confirmation modal state

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

  const handleDeleteChat = async (chatId, event) => {
    event.preventDefault(); // Prevent navigation when clicking delete
    event.stopPropagation();

    // Open the delete confirmation modal
    setDeleteModal({ isOpen: true, chatId });
    setOpenDropdown(null); // Close the dropdown
  };

  const confirmDeleteChat = async () => {
    const { chatId } = deleteModal;

    try {
      await deleteChatById(chatId);

      // If we're currently viewing the deleted chat, navigate to new chat
      if (currentChatId === chatId.toString()) {
        navigate("/chat/new");
      }

      // Refresh the chat list
      dispatch(chatSliceActions.triggerChatRefresh());

      // Close the modal
      setDeleteModal({ isOpen: false, chatId: null });
    } catch (error) {
      console.error("Error deleting chat:", error);
      alert("Failed to delete chat. Please try again.");
    }
  };

  const cancelDeleteChat = () => {
    setDeleteModal({ isOpen: false, chatId: null });
  };

  const toggleDropdown = (chatId, event) => {
    event.preventDefault();
    event.stopPropagation();
    setOpenDropdown(openDropdown === chatId ? null : chatId);
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = () => {
      setOpenDropdown(null);
    };

    document.addEventListener("click", handleClickOutside);
    return () => {
      document.removeEventListener("click", handleClickOutside);
    };
  }, []);

  return (
    <>
      {/* Fixed toggle button - always visible */}
      <button
        className={styles.fixedToggleSidebarBtn}
        onClick={() => dispatch(sidebarActions.toggleSidebar())}
        title={isSidebarOpen ? "Close sidebar" : "Open sidebar"}
      >
        <svg
          width="20"
          height="20"
          viewBox="0 0 20 20"
          fill="currentColor"
          xmlns="http://www.w3.org/2000/svg"
          className="shrink-0"
          aria-hidden="true"
        >
          <path d="M16.5 4C17.3284 4 18 4.67157 18 5.5V14.5C18 15.3284 17.3284 16 16.5 16H3.5C2.67157 16 2 15.3284 2 14.5V5.5C2 4.67157 2.67157 4 3.5 4H16.5ZM7 15H16.5C16.7761 15 17 14.7761 17 14.5V5.5C17 5.22386 16.7761 5 16.5 5H7V15ZM3.5 5C3.22386 5 3 5.22386 3 5.5V14.5C3 14.7761 3.22386 15 3.5 15H6V5H3.5Z"></path>
        </svg>
      </button>

      {isSidebarOpen && (
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
                    <div key={chat.id} className={styles.chatItemWrapper}>
                      <Link
                        to={`/chat/${chat.id}`}
                        className={`${styles.chatItem} ${
                          currentChatId === chat.id.toString()
                            ? styles.activeChatItem
                            : ""
                        }`}
                      >
                        <div className={styles.chatContent}>
                          <div className={styles.chatTitle}>{chat.title}</div>
                          <div className={styles.chatTime}>{chat.time}</div>
                        </div>
                      </Link>
                      <div className={styles.chatActions}>
                        <button
                          className={styles.moreButton}
                          onClick={(e) => toggleDropdown(chat.id, e)}
                          aria-label="More options"
                        >
                          <svg
                            width="16"
                            height="16"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="2"
                          >
                            <circle cx="12" cy="12" r="1" />
                            <circle cx="12" cy="5" r="1" />
                            <circle cx="12" cy="19" r="1" />
                          </svg>
                        </button>
                        {openDropdown === chat.id && (
                          <div className={styles.dropdown}>
                            <button
                              className={styles.dropdownItem}
                              onClick={(e) => handleDeleteChat(chat.id, e)}
                            >
                              <svg
                                width="14"
                                height="14"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="2"
                              >
                                <polyline points="3,6 5,6 21,6" />
                                <path d="M19,6v14a2,2 0,0,1-2,2H7a2,2 0,0,1-2-2V6m3,0V4a2,2 0,0,1,2-2h4a2,2 0,0,1,2,2v2" />
                              </svg>
                              Delete
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
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
      )}

      {/* Delete Confirmation Modal */}
      {deleteModal.isOpen && (
        <div className={styles.modalOverlay} onClick={cancelDeleteChat}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3 className={styles.modalTitle}>Delete chat</h3>
            </div>
            <div className={styles.modalContent}>
              <p className={styles.modalText}>
                Are you sure you want to delete this chat?
              </p>
            </div>
            <div className={styles.modalActions}>
              <button
                className={styles.modalCancelBtn}
                onClick={cancelDeleteChat}
              >
                Cancel
              </button>
              <button
                className={styles.modalDeleteBtn}
                onClick={confirmDeleteChat}
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
