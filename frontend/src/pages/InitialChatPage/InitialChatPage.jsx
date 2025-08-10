import Sidebar from "../../components/Sidebar/Sidebar";
import InitialChat from "../../components/InitialChat/InitialChat";
import styles from "./InitialChatPage.module.css";

export default function InitialChatPage() {
  return (
    <div className={styles.chatPage}>
      <Sidebar />
      <div className={styles.chatContent}>
        <InitialChat />
      </div>
    </div>
  );
}
