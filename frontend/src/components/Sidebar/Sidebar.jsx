import { useSelector, useDispatch } from "react-redux";
import { useSidebar } from "../../hooks/useSidebar";
import { sidebarActions } from "../../store/sidebarSlice";
import SidebarToggle from "./SidebarToggle";
import SidebarHeader from "./SidebarHeader";
import ChatHistory from "./ChatHistory";
import UserSection from "./UserSection";
import DeleteModal from "./DeleteModal";
import RenameModal from "./RenameModal";
import styles from "./Sidebar.module.css";

export default function Sidebar() {
  const dispatch = useDispatch();
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  const {
    chats,
    isLoadingChats,
    openDropdown,
    deleteModal,
    renameModal,
    currentChatId,
    isUserLoggedIn,
    handleNewChat,
    handleLogout,
    handleDeleteChat,
    handleRenameChat,
    confirmRenameChat,
    cancelRenameChat,
    confirmDeleteChat,
    cancelDeleteChat,
    toggleDropdown,
  } = useSidebar();

  const handleBackdropClick = () => {
    dispatch(sidebarActions.closeSidebar());
  };

  const handleCloseSidebar = () => {
    dispatch(sidebarActions.closeSidebar());
  };

  return (
    <>
      <SidebarToggle />

      {/* Mobile backdrop overlay */}
      {isSidebarOpen && (
        <div
          className={styles.backdrop}
          onClick={handleBackdropClick}
          aria-hidden="true"
        />
      )}

      <div
        className={`${styles.sidebar} ${!isSidebarOpen ? styles.closed : ""}`}
      >
        {/* Mobile close button inside sidebar */}
        <button
          className={styles.mobileCloseBtn}
          onClick={handleCloseSidebar}
          title="Close sidebar"
          aria-label="Close sidebar"
        >
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>

        <SidebarHeader
          isUserLoggedIn={isUserLoggedIn}
          onNewChat={handleNewChat}
        />

        {isUserLoggedIn && (
          <ChatHistory
            chats={chats}
            isLoadingChats={isLoadingChats}
            currentChatId={currentChatId}
            openDropdown={openDropdown}
            onToggleDropdown={toggleDropdown}
            onDeleteChat={handleDeleteChat}
            onRenameChat={handleRenameChat}
          />
        )}

        <UserSection isUserLoggedIn={isUserLoggedIn} onLogout={handleLogout} />
      </div>

      <DeleteModal
        isOpen={deleteModal.isOpen}
        onConfirm={confirmDeleteChat}
        onCancel={cancelDeleteChat}
      />

      <RenameModal
        isOpen={renameModal.isOpen}
        chatTitle={renameModal.chatTitle}
        onConfirm={confirmRenameChat}
        onCancel={cancelRenameChat}
      />
    </>
  );
}
