import { useState } from "react";
import DeleteConfirmModal from "../DeleteConfirmModal/DeleteConfirmModal";
import styles from "./NewsletterSection.module.css";

export default function NewsletterSection({
  isSubscribed,
  onToggle,
  isUpdating,
  message,
}) {
  const [showConfirmModal, setShowConfirmModal] = useState(false);

  const handleToggleClick = () => {
    setShowConfirmModal(true);
  };

  const handleConfirm = async () => {
    setShowConfirmModal(false);
    await onToggle();
  };

  const handleCancel = () => {
    setShowConfirmModal(false);
  };

  return (
    <>
      <div className={styles.section}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>Newsletter Subscription</h2>
          <p className={styles.sectionDescription}>
            Manage your newsletter preferences and stay updated with the latest
            news
          </p>
        </div>

        <div className={styles.subscriptionStatus}>
          <div className={styles.statusInfo}>
            <div className={styles.statusBadge}>
              {isSubscribed ? (
                <>
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    className={styles.statusIcon}
                  >
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                  <span>Subscribed</span>
                </>
              ) : (
                <>
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    className={styles.statusIcon}
                  >
                    <line x1="18" y1="6" x2="6" y2="18" />
                    <line x1="6" y1="6" x2="18" y2="18" />
                  </svg>
                  <span>Not subscribed</span>
                </>
              )}
            </div>
            <p className={styles.statusText}>
              {isSubscribed
                ? "You're receiving our newsletter with updates and tips"
                : "You're not receiving our newsletter"}
            </p>
          </div>

          <button
            type="button"
            onClick={handleToggleClick}
            disabled={isUpdating}
            className={`${styles.toggleButton} ${
              isSubscribed ? styles.unsubscribe : styles.subscribe
            }`}
          >
            {isUpdating
              ? "Updating..."
              : isSubscribed
              ? "Unsubscribe"
              : "Subscribe"}
          </button>
        </div>

        {message && (
          <div
            className={`${styles.message} ${
              message.includes("success") ? styles.success : styles.error
            }`}
          >
            {message}
          </div>
        )}
      </div>

      {showConfirmModal && (
        <DeleteConfirmModal
          title={
            isSubscribed
              ? "Unsubscribe from Newsletter"
              : "Subscribe to Newsletter"
          }
          message={
            isSubscribed
              ? "Are you sure you want to unsubscribe from our newsletter? You'll no longer receive updates and tips."
              : "Would you like to subscribe to our newsletter? You'll receive updates and tips regularly."
          }
          onConfirm={handleConfirm}
          onCancel={handleCancel}
          confirmText={isSubscribed ? "Unsubscribe" : "Subscribe"}
        />
      )}
    </>
  );
}
