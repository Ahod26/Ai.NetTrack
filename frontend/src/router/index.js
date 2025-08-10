import { createBrowserRouter, Navigate } from "react-router-dom";
import App from "../App";
import ChatPage from "../pages/ChatPage/ChatPage";
import TimelinePage from "../pages/TimelinePage/TimelinePage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      {
        index: true,
        element: <Navigate to="/chat" replace />,
      },
      {
        path: "chat",
        element: <ChatPage />,
      },
      {
        path: "timeline",
        element: <TimelinePage />,
      },
    ],
  },
]);
