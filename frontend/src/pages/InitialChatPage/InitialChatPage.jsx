import { useState, useEffect } from "react";
import { useLocation } from "react-router-dom";
import InitialChat from "../../components/Chat/InitialChat/InitialChat";
import ErrorPopup from "../../components/ErrorPopup";
import styles from "./InitialChatPage.module.css";

export default function InitialChatPage() {
  const location = useLocation();
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    // Check if there's an error from navigation state (e.g., from Google login failure)
    if (location.state?.error) {
      setErrorMessage(location.state.error);

      // Clear the error after 5 seconds
      const timer = setTimeout(() => {
        setErrorMessage("");
      }, 5000);

      // Clear navigation state to prevent showing error again on refresh
      window.history.replaceState({}, document.title);

      return () => clearTimeout(timer);
    }
  }, [location]);

  return (
    <div className={styles.chatPage}>
      <div className={styles.chatContent}>
        <InitialChat />
      </div>
      <ErrorPopup message={errorMessage} />
    </div>
  );
}
