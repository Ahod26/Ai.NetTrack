import styles from "./AccountSettings.module.css";

export default function FullNameSection({
  fullName,
  setFullName,
  onSubmit,
  isUpdating,
  message,
  currentUserFullName,
}) {
  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>Full Name</h2>
        <p className={styles.sectionDescription}>Update your display name</p>
      </div>
      <form onSubmit={onSubmit} className={styles.form}>
        <div className={styles.inputGroup}>
          <input
            type="text"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            className={styles.input}
            placeholder="Enter your full name"
          />
        </div>
        {message.text && (
          <div className={`${styles.message} ${styles[message.type]}`}>
            {message.text}
          </div>
        )}
        <button
          type="submit"
          className={styles.saveBtn}
          disabled={isUpdating || fullName === currentUserFullName}
        >
          {isUpdating ? "Updating..." : "Update Full Name"}
        </button>
      </form>
    </section>
  );
}
