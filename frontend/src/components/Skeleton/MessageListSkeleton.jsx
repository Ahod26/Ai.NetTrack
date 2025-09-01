import Skeleton from "../Skeleton/Skeleton";
import styles from "./MessageListSkeleton.module.css";
import messageStyles from "../Chat/MessageList/MessageList.module.css";

const MessageListSkeleton = ({
  count = 3,
  showStarredActions = false,
  inline = false,
}) => {
  const Wrapper = ({ children }) =>
    inline ? (
      <>{children}</>
    ) : (
      <div className={styles.messageListSkeleton}>{children}</div>
    );

  return (
    <Wrapper>
      {Array.from({ length: count }, (_, index) => (
        <div
          key={index}
          className={`${
            inline ? messageStyles.message : styles.messageSkeleton
          } ${inline ? messageStyles.assistant : ""}`}
        >
          <div
            className={
              inline ? messageStyles.messageContent : styles.messageContent
            }
          >
            <Skeleton
              variant="text"
              width="92%"
              height="15px"
              animation="wave"
            />
            <Skeleton
              variant="text"
              width="85%"
              height="15px"
              animation="wave"
            />
            <Skeleton
              variant="text"
              width="55%"
              height="15px"
              animation="wave"
            />
          </div>
          {showStarredActions && (
            <div
              className={
                inline ? messageStyles.messageActions : styles.messageActions
              }
            >
              <Skeleton variant="circle" width="24px" height="24px" />
              <Skeleton variant="circle" width="24px" height="24px" />
              <Skeleton variant="circle" width="24px" height="24px" />
            </div>
          )}
        </div>
      ))}
    </Wrapper>
  );
};

export default MessageListSkeleton;
