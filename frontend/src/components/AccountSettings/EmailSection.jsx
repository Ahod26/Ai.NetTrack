import styles from "./AccountSettings.module.css";

export default function EmailSection({
  email,
  setEmail,
  onSubmit,
  isUpdating,
  message,
  currentUserEmail,
}) {
  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>Email Address</h2>
        <p className={styles.sectionDescription}>Change your email address</p>
      </div>
      <form onSubmit={onSubmit} className={styles.form}>
        <div className={styles.inputGroup}>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={styles.input}
            placeholder="Enter your email"
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
          disabled={isUpdating || email === currentUserEmail}
        >
          {isUpdating ? "Updating..." : "Update Email"}
        </button>
      </form>
    </section>
  );
}
