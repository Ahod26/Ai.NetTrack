import { Link, useLocation } from "react-router-dom";
import { useSelector } from "react-redux";
import styles from "./Header.module.css";

export default function Header() {
  const location = useLocation();
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  return (
    <header
      className={`${styles.header} ${
        !isSidebarOpen ? styles.sidebarClosed : ""
      }`}
    >
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
