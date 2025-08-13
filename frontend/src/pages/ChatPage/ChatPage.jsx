import Sidebar from "../../components/Sidebar/Sidebar";
import MainChat from "../../components/Chat/MainChat/MainChat";
import styles from "./ChatPage.module.css";

export default function ChatPage() {
  return (
    <div className={styles.chatPage}>
      <Sidebar />
      <div className={styles.chatContent}>
        <MainChat />
      </div>
    </div>
  );
}
