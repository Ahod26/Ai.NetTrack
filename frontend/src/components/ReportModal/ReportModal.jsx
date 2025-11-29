import { useState, useEffect, useRef } from "react";
import styles from "./ReportModal.module.css";

export default function ReportModal({ messageId, onClose, onSubmit }) {
  const [reportText, setReportText] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const textareaRef = useRef(null);

  useEffect(() => {
    // Focus textarea when modal opens
    textareaRef.current?.focus();

    // Prevent body scroll when modal is open
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = "unset";
    };
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!reportText.trim() || isSubmitting) {
      return;
    }

    setIsSubmitting(true);

    try {
      await onSubmit(messageId, reportText.trim());
      onClose();
    } catch (error) {
      console.error("Failed to submit report:", error);
      setIsSubmitting(false);
    }
  };

  const handleBackdropClick = (e) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === "Escape") {
      onClose();
    }
  };

  return (
    <div
      className={styles.modalBackdrop}
      onClick={handleBackdropClick}
      onKeyDown={handleKeyDown}
    >
      <div className={styles.modalContent}>
        <div className={styles.modalHeader}>
          <h3>Report Message</h3>
          <button
            className={styles.closeButton}
            onClick={onClose}
            aria-label="Close modal"
          >
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className={styles.modalBody}>
            <label htmlFor="reportText" className={styles.label}>
              Please describe the issue with this message:
            </label>
            <textarea
              ref={textareaRef}
              id="reportText"
              className={styles.textarea}
              value={reportText}
              onChange={(e) => setReportText(e.target.value)}
              placeholder="E.g., The response contains inaccurate information..."
              rows={5}
              disabled={isSubmitting}
            />
          </div>

          <div className={styles.modalFooter}>
            <button
              type="button"
              className={styles.cancelButton}
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className={styles.submitButton}
              disabled={!reportText.trim() || isSubmitting}
            >
              {isSubmitting ? "Submitting..." : "Submit Report"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
