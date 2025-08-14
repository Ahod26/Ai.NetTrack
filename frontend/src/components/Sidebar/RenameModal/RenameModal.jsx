import { useState, useEffect } from "react";
import styles from "../Sidebar.module.css";

export default function RenameModal({
  isOpen,
  chatTitle,
  onConfirm,
  onCancel,
}) {
  const [newTitle, setNewTitle] = useState(chatTitle);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationError, setValidationError] = useState("");
  const [serverError, setServerError] = useState("");

  // Reset states when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setNewTitle(chatTitle);
      setValidationError("");
      setServerError("");
    }
  }, [isOpen, chatTitle]);

  // Client-side validation function
  const validateTitle = (title) => {
    const trimmedTitle = title.trim();

    if (!trimmedTitle) {
      return "Title is required";
    }

    if (trimmedTitle.length < 1) {
      return "Title must be at least 1 character";
    }

    if (trimmedTitle.length > 20) {
      return "Title must be 20 characters or less";
    }

    return "";
  };

  // Handle input change with real-time validation
  const handleTitleChange = (e) => {
    const value = e.target.value;
    setNewTitle(value);

    // Clear server error when user starts typing
    if (serverError) {
      setServerError("");
    }

    // Validate in real-time
    const error = validateTitle(value);
    setValidationError(error);
  };

  const handleSubmit = async () => {
    const trimmedTitle = newTitle.trim();

    // Final validation check
    const validationError = validateTitle(newTitle);
    if (validationError) {
      setValidationError(validationError);
      return;
    }

    // Check if title actually changed
    if (trimmedTitle === chatTitle.trim()) {
      onCancel();
      return;
    }

    setIsSubmitting(true);
    setServerError("");

    try {
      await onConfirm(trimmedTitle);
    } catch (error) {
      // Handle server errors
      let errorMessage = "Failed to rename chat. Please try again.";

      if (error.message) {
        errorMessage = error.message;
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error.response?.data?.errors) {
        // Handle validation errors from server
        const errors = error.response.data.errors;
        if (errors.title && Array.isArray(errors.title)) {
          errorMessage = errors.title[0];
        } else if (typeof errors === "string") {
          errorMessage = errors;
        }
      }

      setServerError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      handleSubmit();
    }
  };

  if (!isOpen) return null;

  // Determine if save button should be disabled
  const isSaveDisabled =
    isSubmitting ||
    !!validationError ||
    !newTitle.trim() ||
    newTitle.trim() === chatTitle.trim();

  return (
    <div className={styles.modalOverlay} onClick={onCancel}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h3 className={styles.modalTitle}>Rename chat</h3>
        </div>
        <div className={styles.modalContent}>
          <input
            type="text"
            value={newTitle}
            onChange={handleTitleChange}
            className={`${styles.renameInput} ${
              validationError || serverError ? styles.inputError : ""
            }`}
            placeholder="Enter new title"
            autoFocus
            disabled={isSubmitting}
            maxLength={20}
            onKeyPress={handleKeyPress}
          />

          {/* Error Messages */}
          {(validationError || serverError) && (
            <div className={styles.errorMessage}>
              {validationError || serverError}
            </div>
          )}

          {/* Character Count */}
          <div className={styles.characterCount}>
            {newTitle.length}/20 characters
          </div>
        </div>
        <div className={styles.modalActions}>
          <button
            className={styles.modalCancelBtn}
            onClick={onCancel}
            disabled={isSubmitting}
          >
            Cancel
          </button>
          <button
            className={styles.modalSaveBtn}
            onClick={handleSubmit}
            disabled={isSaveDisabled}
          >
            {isSubmitting ? "Saving..." : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
}
