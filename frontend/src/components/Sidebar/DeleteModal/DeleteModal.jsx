import styles from "../Sidebar.module.css";

export default function DeleteModal({ isOpen, onConfirm, onCancel }) {
  if (!isOpen) return null;

  return (
    <div className={styles.modalOverlay} onClick={onCancel}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        {/* stop propagation prevent the click in the modal effecting the background */}
        <div className={styles.modalHeader}>
          <h3 className={styles.modalTitle}>Delete chat</h3>
        </div>
        <div className={styles.modalContent}>
          <p className={styles.modalText}>
            Are you sure you want to delete this chat?
          </p>
        </div>
        <div className={styles.modalActions}>
          <button className={styles.modalCancelBtn} onClick={onCancel}>
            Cancel
          </button>
          <button className={styles.modalDeleteBtn} onClick={onConfirm}>
            Delete
          </button>
        </div>
      </div>
    </div>
  );
}
