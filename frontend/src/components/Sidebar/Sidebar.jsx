import { useSelector } from "react-redux";
import { useSidebar } from "../../hooks/useSidebar";
import SidebarToggle from "./SidebarToggle";
import SidebarHeader from "./SidebarHeader";
import ChatHistory from "./ChatHistory";
import UserSection from "./UserSection";
import DeleteModal from "./DeleteModal";
import RenameModal from "./RenameModal";
import styles from "./Sidebar.module.css";

export default function Sidebar() {
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

  return (
    <>
      <SidebarToggle />

      <div
        className={`${styles.sidebar} ${!isSidebarOpen ? styles.closed : ""}`}
      >
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
