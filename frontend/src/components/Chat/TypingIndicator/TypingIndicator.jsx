import styles from "./TypingIndicator.module.css";

export default function TypingIndicator({ label = "Thinking" }) {
  return (
    <div className={styles.thinkingWrapper}>
      <div className={styles.thinkingBubble} role="status" aria-live="polite">
        <span className={styles.label}>{label}</span>
        <span className={styles.dots}>
          <span className={styles.dot} />
          <span className={styles.dot} />
          <span className={styles.dot} />
        </span>
      </div>
    </div>
  );
}
