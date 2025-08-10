import { Link, useLocation } from "react-router-dom";
import styles from "./Header.module.css";

export default function Header() {
  const location = useLocation();

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <nav className={styles.navigation}>
          <Link
            to="/chat/new"
            className={`${styles.navLink} ${
              location.pathname.startsWith("/chat") || location.pathname === "/"
                ? styles.active
                : ""
            }`}
          >
            Chat
          </Link>
          <Link
            to="/timeline"
            className={`${styles.navLink} ${
              location.pathname === "/timeline" ? styles.active : ""
            }`}
          >
            Timeline
          </Link>
        </nav>
      </div>
    </header>
  );
}
