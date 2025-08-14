import styles from "./ChatActionsMenu.module.css";

export default function ChatActionsMenu({
  chat,
  isOpen,
  onClose,
  onDelete,
  onRename,
}) {
  const handleRenameClick = () => {
    onRename(chat.id);
    onClose();
  };

  const handleDeleteClick = () => {
    onDelete(chat.id);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className={styles.dropdown}>
      <button className={styles.dropdownItem} onClick={handleRenameClick}>
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
          <path d="M18.5 2.5a2.12 2.12 0 0 1 3 3L12 15l-4 1 1-4z" />
        </svg>
        Rename
      </button>
      <button className={styles.dropdownItem} onClick={handleDeleteClick}>
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
  );
}
