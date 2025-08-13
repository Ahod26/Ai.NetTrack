import MarkdownRenderer from "../../MarkdownRenderer/MarkdownRenderer";
import styles from "./MessageList.module.css";

export default function MessageList({
  messages,
  isSendingMessage,
  messagesContainerRef,
  onScroll,
}) {
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
      {isSendingMessage && (
        <div className={`${styles.message} ${styles.assistant}`}>
          <div className={styles.messageContent}>Thinking...</div>
        </div>
      )}
    </div>
  );
}
