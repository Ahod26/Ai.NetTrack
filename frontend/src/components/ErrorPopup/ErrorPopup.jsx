import { createPortal } from "react-dom";
import styles from "./ErrorPopup.module.css";

export default function ErrorPopup({ message }) {
  if (!message) {
    return null;
  }

  // Render the popup directly in document.body, otherwise it wont work in the main chat component
  return createPortal(
    <div className={styles.errorBox}>
      {message}
      <div className={styles.timerBar}></div>
    </div>,
    document.body
  );
}
