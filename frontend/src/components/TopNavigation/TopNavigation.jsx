import { Link, useLocation } from "react-router-dom";
import {  } from "framer-motion";
import styles from "./TopNavigation.module.css";

const navItems = [
  { path: "/chat/new", label: "Chat", id: "chat" },
  { path: "/timeline", label: "Timeline", id: "timeline" },
];

export default function TopNavigation() {
  const location = useLocation();

  // Determine active tab based on path
  const activeTab =
    navItems.find((item) => {
      if (item.id === "chat")
        return (
          location.pathname.startsWith("/chat") || location.pathname === "/"
        );
      return location.pathname.startsWith(item.path);
    })?.id || "chat";

  return (
    <div className={styles.topNavContainer}>
      <nav className={styles.nav}>
        {navItems.map((item) => (
          <Link
            key={item.id}
            to={item.path}
            className={`${styles.navItem} ${
              activeTab === item.id ? styles.active : ""
            }`}
          >
            {activeTab === item.id && (
              <motion.div
                layoutId="activeTab"
                className={styles.activeBackground}
                transition={{ type: "spring", bounce: 0.2, duration: 0.6 }}
              />
            )}
            <span className={styles.navLabel}>{item.label}</span>
          </Link>
        ))}
      </nav>
    </div>
  );
}
