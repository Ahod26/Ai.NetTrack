import { Outlet } from "react-router-dom";
import Header from "./components/Header/Header";
import "./App.css";

export default function App() {
  return (
    <div className="app">
      <Header />
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
