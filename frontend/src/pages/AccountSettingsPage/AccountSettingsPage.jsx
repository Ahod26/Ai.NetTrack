import DeleteConfirmModal from "../../components/DeleteConfirmModal/DeleteConfirmModal";
import ErrorPopup from "../../components/ErrorPopup";
import FullNameSection from "../../components/AccountSettings/FullNameSection";
import EmailSection from "../../components/AccountSettings/EmailSection";
import PasswordSection from "../../components/AccountSettings/PasswordSection";
import DangerZoneSection from "../../components/AccountSettings/DangerZoneSection";
import { useAccountSettings } from "../../hooks/useAccountSettings";
import styles from "./AccountSettingsPage.module.css";

export default function AccountSettingsPage() {
  const {
    user,
    fullName,
    setFullName,
    email,
    setEmail,
    currentPassword,
    setCurrentPassword,
    newPassword,
    setNewPassword,
    showCurrentPassword,
    setShowCurrentPassword,
    showNewPassword,
    setShowNewPassword,
    isUpdatingFullName,
    isUpdatingEmail,
    isUpdatingPassword,
    fullNameMessage,
    emailMessage,
    passwordMessage,
    showDeleteModal,
    setShowDeleteModal,
    handleUpdateFullName,
    handleUpdateEmail,
    handleUpdatePassword,
    handleDeleteAccount,
    handleBack,
    rateLimitError,
  } = useAccountSettings();

  return (
    <div className={styles.container}>
      <button
        onClick={handleBack}
        className={styles.backButton}
        aria-label="Go back"
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
          <path d="M19 12H5M12 19l-7-7 7-7" />
        </svg>
        Back
      </button>

      <div className={styles.header}>
        <h1 className={styles.title}>Account Settings</h1>
        <p className={styles.subtitle}>
          Manage your account information and preferences
        </p>
      </div>

      <div className={styles.sections}>
        <FullNameSection
          fullName={fullName}
          setFullName={setFullName}
          onSubmit={handleUpdateFullName}
          isUpdating={isUpdatingFullName}
          message={fullNameMessage}
          currentUserFullName={user?.fullName}
        />

        <EmailSection
          email={email}
          setEmail={setEmail}
          onSubmit={handleUpdateEmail}
          isUpdating={isUpdatingEmail}
          message={emailMessage}
          currentUserEmail={user?.email}
        />

        <PasswordSection
          currentPassword={currentPassword}
          setCurrentPassword={setCurrentPassword}
          newPassword={newPassword}
          setNewPassword={setNewPassword}
          showCurrentPassword={showCurrentPassword}
          setShowCurrentPassword={setShowCurrentPassword}
          showNewPassword={showNewPassword}
          setShowNewPassword={setShowNewPassword}
          onSubmit={handleUpdatePassword}
          isUpdating={isUpdatingPassword}
          message={passwordMessage}
        />

        <DangerZoneSection onDeleteClick={() => setShowDeleteModal(true)} />
      </div>

      {showDeleteModal && (
        <DeleteConfirmModal
          title="Delete Account"
          message="Are you sure you want to delete your account? This action cannot be undone and all your data will be permanently deleted."
          onConfirm={handleDeleteAccount}
          onCancel={() => setShowDeleteModal(false)}
          confirmText="Delete Account"
        />
      )}

      <ErrorPopup message={rateLimitError} />
    </div>
  );
}
