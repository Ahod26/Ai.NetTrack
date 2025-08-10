import { Link, useLocation } from "react-router-dom";
import styles from "./Header.module.css";

const Header = () => {
  const location = useLocation();

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <div className={styles.logo}>
          <h1>.Net AI Developers</h1>
        </div>

        <nav className={styles.navigation}>
          <Link
            to="/chat"
            className={`${styles.navLink} ${
              location.pathname === "/chat" || location.pathname === "/"
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
};

export default Header;
