import styles from "./DeleteConfirmModal.module.css";

export default function DeleteConfirmModal({
  title = "Confirm Delete",
  message,
  onConfirm,
  onCancel,
  confirmText = "Delete",
}) {
  return (
    <div className={styles.modalOverlay} onClick={onCancel}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h3 className={styles.modalTitle}>{title}</h3>
        </div>
        <div className={styles.modalContent}>
          <p className={styles.modalText}>{message}</p>
        </div>
        <div className={styles.modalActions}>
          <button className={styles.modalCancelBtn} onClick={onCancel}>
            Cancel
          </button>
          <button className={styles.modalDeleteBtn} onClick={onConfirm}>
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  );
}
