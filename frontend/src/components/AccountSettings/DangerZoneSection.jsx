import styles from "./AccountSettings.module.css";

export default function DangerZoneSection({ onDeleteClick }) {
  return (
    <section className={`${styles.section} ${styles.dangerSection}`}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>Danger Zone</h2>
        <p className={styles.sectionDescription}>
          Permanently delete your account and all associated data
        </p>
      </div>
      <button onClick={onDeleteClick} className={styles.deleteBtn}>
        Delete Account
      </button>
    </section>
  );
}
