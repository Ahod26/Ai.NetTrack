import { Outlet } from "react-router-dom";
import { useSelector } from "react-redux";
import Sidebar from "./components/Sidebar/Sidebar";
import TopNavigation from "./components/TopNavigation/TopNavigation";
import AuthProvider from "./contexts/AuthProvider";
import "./App.css";

export default function App() {
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  return (
    <AuthProvider>
      <div className="app">
        <Sidebar />
        <main
          className="main-content"
          style={{
            marginLeft: isSidebarOpen ? "216px" : "0",
            transition: "margin-left 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
            display: "flex",
            flexDirection: "column",
            height: "100vh",
            overflow: "hidden",
          }}
        >
          <TopNavigation />
          <div style={{ flex: 1, overflow: "hidden", position: "relative" }}>
            <Outlet />
          </div>
        </main>
      </div>
    </AuthProvider>
  );
}
