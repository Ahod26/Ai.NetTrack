import { Outlet } from "react-router-dom";
import Header from "./components/Header/Header";
import AuthProvider from "./contexts/AuthProvider";
import "./App.css";

export default function App() {
  return (
    <AuthProvider>
      <div className="app">
        <Header />
        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </AuthProvider>
  );
}
