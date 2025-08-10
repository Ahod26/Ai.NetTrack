import { Navigate } from "react-router-dom";
import styles from "./NotFound.module.css";

export default function NotFound() {
  return (
    <div className={styles.notFound}>
      <h2>Page Not Found</h2>
      <p>The page you're looking for doesn't exist.</p>
      <Navigate to="/chat" replace />
    </div>
  );
}
