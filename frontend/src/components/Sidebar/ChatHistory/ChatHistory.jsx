import { memo } from "react";
import { Link } from "react-router-dom";
import ChatActionsMenu from "../ChatActionsMenu";
import styles from "../Sidebar.module.css";

const ChatHistory = memo(function ChatHistory({
  chats,
  isLoadingChats,
  currentChatId,
  openDropdown,
  onToggleDropdown,
  onDeleteChat,
  onRenameChat,
}) {
  if (!chats || isLoadingChats) {
    return (
      <div className={styles.chatHistory}>
        <div className={styles.sectionTitle}>Recent Chats</div>
        <div className={styles.chatList}>
          <div className={styles.loadingChats}>Loading chats...</div>
        </div>
      </div>
    );
  }

  if (chats.length === 0) {
    return (
      <div className={styles.chatHistory}>
        <div className={styles.sectionTitle}>Recent Chats</div>
        <div className={styles.chatList}>
          <div className={styles.noChats}>
            No chats yet. Start a new conversation!
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.chatHistory}>
      <div className={styles.sectionTitle}>Recent Chats</div>
      <div className={styles.chatList}>
        {chats.map((chat) => {
          const isActive = currentChatId === chat.id.toString();
          const chatItemClassName = `${styles.chatItem} ${
            isActive ? styles.activeChatItem : ""
          }`;

          return (
            <div key={chat.id} className={styles.chatItemWrapper}>
              <Link to={`/chat/${chat.id}`} className={chatItemClassName}>
                <div className={styles.chatContent}>
                  <div className={styles.chatTitle}>{chat.title}</div>
                  <div className={styles.chatTime}>{chat.time}</div>
                </div>
              </Link>
              <div className={styles.chatActions}>
                <button
                  className={styles.moreButton}
                  onClick={(e) => onToggleDropdown(chat.id, e)}
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
                <ChatActionsMenu
                  chat={chat}
                  isOpen={openDropdown === chat.id}
                  onClose={() => onToggleDropdown(null)}
                  onDelete={onDeleteChat}
                  onRename={onRenameChat}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
});

export default ChatHistory;
