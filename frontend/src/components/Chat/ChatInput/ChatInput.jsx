import { useState, useRef } from "react";
import styles from "./ChatInput.module.css";

export default function ChatInput({
  onSendMessage,
  placeholder = "Ask anything...",
  disabled = false,
}) {
  const [message, setMessage] = useState("");
  const textareaRef = useRef(null);

  const handleMessageChange = (e) => {
    if (disabled) return;
    setMessage(e.target.value);

    // Auto-resize textarea
    const textarea = e.target;
    textarea.style.height = "auto";
    textarea.style.height =
      Math.min(textarea.scrollHeight, window.innerHeight * 0.25) + "px";
  };

  const handleKeyDown = (e) => {
    if (disabled) return;
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage(e);
    }
  };

  const handleSendMessage = (e) => {
    e.preventDefault();
    if (disabled || !message.trim()) return;

    onSendMessage(message.trim());
    setMessage("");

    // Reset textarea height to original size after DOM update
    setTimeout(() => {
      if (textareaRef.current) {
        textareaRef.current.style.height = "";
        textareaRef.current.style.height = "auto";
      }
    }, 0);
  };

  const handleButtonClick = (e) => {
    e.preventDefault();
    handleSendMessage(e);
  };

  return (
    <div className={styles.inputContainer}>
      <form onSubmit={handleSendMessage} className={styles.inputForm}>
        <div className={styles.inputWrapper}>
          <textarea
            ref={textareaRef}
            value={message}
            onChange={handleMessageChange}
            onKeyDown={handleKeyDown}
            placeholder={placeholder}
            className={styles.messageInput}
            rows={1}
            disabled={disabled}
          />
          <button
            type="button"
            className={styles.sendButton}
            onClick={handleButtonClick}
            disabled={disabled || !message.trim()}
          >
            {/* Send icon - arrow */}
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M22 2L11 13M22 2l-7 20-4-9-9-4 20-7z" />
            </svg>
          </button>
        </div>
      </form>
    </div>
  );
}
