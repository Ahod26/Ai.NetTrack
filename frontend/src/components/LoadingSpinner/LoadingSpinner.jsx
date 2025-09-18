import styles from "./LoadingSpinner.module.css";

export default function LoadingSpinner({ size = "medium", text = "" }) {
  const sizeClass =
    {
      small: styles.small,
      medium: styles.medium,
      large: styles.large,
    }[size] || styles.medium;

  return (
    <div className={styles.loadingContainer}>
      <div className={`${styles.spinner} ${sizeClass}`}>
        <div className={styles.spinnerInner}></div>
      </div>
      {text && <p className={styles.loadingText}>{text}</p>}
    </div>
  );
}
