import Skeleton from "../Skeleton/Skeleton";
import styles from "./ChatListSkeleton.module.css";
import sidebarStyles from "../Sidebar/Sidebar.module.css";

const ChatListSkeleton = ({ count = 5 }) => {
  return (
    <div className={styles.chatListSkeleton}>
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className={sidebarStyles.chatItemWrapper}>
          <div className={sidebarStyles.chatItem}>
            <div className={sidebarStyles.chatContent}>
              <Skeleton
                variant="text"
                width="80%"
                height="14px"
                className={styles.titleSkeleton}
                animation="wave"
              />
              <Skeleton
                variant="text"
                width="45%"
                height="11px"
                className={styles.timeSkeleton}
                animation="wave"
              />
            </div>
          </div>
          <div className={sidebarStyles.chatActions}>
            <Skeleton
              variant="circle"
              width="16px"
              height="16px"
              animation="pulse"
            />
          </div>
        </div>
      ))}
    </div>
  );
};

export default ChatListSkeleton;
