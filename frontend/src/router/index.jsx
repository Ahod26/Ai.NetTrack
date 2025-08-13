import { createBrowserRouter, Navigate } from "react-router-dom";
import App from "../App";
import ChatPage from "../pages/ChatPage/ChatPage";
import InitialChatPage from "../pages/InitialChatPage/InitialChatPage";
import TimelinePage from "../pages/TimelinePage/TimelinePage";
import NotFound from "../pages/NotFound/NotFound";
import { Login, Signup } from "../components/Auth";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    errorElement: (
      <div style={{ padding: "2rem", textAlign: "center" }}>
        <h2>Something went wrong!</h2>
        <p>Please try refreshing the page.</p>
      </div>
    ),
    children: [
      {
        index: true,
        element: <Navigate to="/chat/new" replace />,
      },
      {
        path: "chat/new",
        element: <InitialChatPage />,
      },
      {
        path: "chat/:chatId",
        element: <ChatPage />,
      },
      {
        path: "login",
        element: <Login />,
      },
      {
        path: "signup",
        element: <Signup />,
      },
      {
        path: "timeline",
        element: <TimelinePage />,
      },
      {
        path: "*",
        element: <NotFound />,
      },
    ],
  },
]);
