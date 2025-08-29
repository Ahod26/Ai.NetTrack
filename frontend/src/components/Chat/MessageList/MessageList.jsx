import { memo } from "react";
import MarkdownRenderer from "../../MarkdownRenderer/MarkdownRenderer";
import styles from "./MessageList.module.css";

const MessageList = memo(function MessageList({
  messages,
  isSendingMessage,
  messagesContainerRef,
  onScroll,
}) {
  const hasChunkMessage = messages.some((msg) => msg.isChunkMessage);
  return (
    <div
      className={styles.messagesContainer}
      ref={messagesContainerRef}
      onScroll={onScroll}
    >
      {messages.map((msg) => (
        <div key={msg.id} className={`${styles.message} ${styles[msg.type]}`}>
          <div className={styles.messageContent}>
            {msg.type === "assistant" ? (
              <MarkdownRenderer content={msg.content} />
            ) : (
              msg.content
            )}
          </div>
        </div>
      ))}
      {isSendingMessage && !hasChunkMessage && (
        <div className={`${styles.message} ${styles.assistant}`}>
          <div className={styles.messageContent}>Thinking...</div>
        </div>
      )}
    </div>
  );
});

export default MessageList;
