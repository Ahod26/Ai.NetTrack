import { useEffect } from "react";
import { useSelector, useDispatch } from "react-redux";
import { sidebarActions } from "../../store/sidebarSlice";
import styles from "./Timeline.module.css";

export default function Timeline() {
  const dispatch = useDispatch();
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  // Close sidebar when Timeline component mounts
  useEffect(() => {
    dispatch(sidebarActions.closeSidebar());
  }, [dispatch]);

  return (
    <div
      className={`${styles.timelineContainer} ${
        !isSidebarOpen ? styles.sidebarClosed : ""
      }`}
    >
      <div className={styles.content}>
        <h1 className={styles.title}>Timeline</h1>
        <p className={styles.subtitle}>
          Coming soon - View your conversation history in a timeline format.
        </p>
      </div>
    </div>
  );
}
